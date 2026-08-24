using System.Net;
using System.Security.Claims;
using LabManagementAPI.Data;
using LabManagementAPI.Hubs;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BorrowController : ControllerBase
{
    private const string Pending = BorrowStatuses.Pending;
    private const string TeacherPending = BorrowStatuses.TeacherPending;
    private const string Borrowed = BorrowStatuses.Borrowed;
    private const string Rejected = BorrowStatuses.Rejected;
    private const string ProcessingApproval = BorrowStatuses.ProcessingApproval;
    private const string ProcessingReturn = BorrowStatuses.ReturnProcessing;

    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IAuditService _auditService;

    public BorrowController(
        AppDbContext context,
        IEmailService emailService,
        IHubContext<NotificationHub> hubContext,
        IAuditService auditService)
    {
        _context = context;
        _emailService = emailService;
        _hubContext = hubContext;
        _auditService = auditService;
    }

    public sealed class BorrowRequestDto
    {
        public int EquipmentId { get; set; }
        public DateTime ExpectedReturnDate { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public int? TeacherId { get; set; }
    }

    public sealed class ReturnInspectionDto
    {
        public string Condition { get; set; } = EquipmentStatuses.Available;
        public string Note { get; set; } = string.Empty;
        public decimal CompensationAmount { get; set; }
    }

    public sealed class ReportDamageDto
    {
        public string Reason { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = Roles.Borrowers)]
    public async Task<ActionResult> CreateRequest(
        [FromBody] BorrowRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var role = User.FindFirstValue(ClaimTypes.Role);
        request.Purpose = request.Purpose.Trim();

        if (request.EquipmentId <= 0)
        {
            return BadRequest(new { message = "Tài sản không hợp lệ." });
        }

        if (request.ExpectedReturnDate <= DateTime.UtcNow)
        {
            return BadRequest(new { message = "Hạn trả dự kiến phải ở tương lai." });
        }

        if (string.IsNullOrWhiteSpace(request.Purpose) || request.Purpose.Length > 1000)
        {
            return BadRequest(new { message = "Mục đích mượn là bắt buộc và không vượt quá 1000 ký tự." });
        }

        if (role == Roles.Teacher)
        {
            request.TeacherId = null;
        }
        else if (role == Roles.Student && !request.TeacherId.HasValue)
        {
            return BadRequest(new { message = "Sinh viên bắt buộc phải chọn giảng viên bảo lãnh." });
        }
        else if (request.TeacherId.HasValue)
        {
            var isValidTeacher = await _context.Users
                .AnyAsync(
                    user => user.Id == request.TeacherId.Value
                        && user.Role == Roles.Teacher
                        && user.IsActive,
                    cancellationToken);
            if (!isValidTeacher)
            {
                return BadRequest(new { message = "Giảng viên bảo lãnh không hợp lệ." });
            }
        }

        var equipment = await _context.Equipments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == request.EquipmentId
                    && item.Status == EquipmentStatuses.Available,
                cancellationToken);
        if (equipment is null)
        {
            return BadRequest(new { message = "Tài sản không tồn tại hoặc không sẵn sàng để mượn." });
        }

        var hasDuplicate = await _context.BorrowRecords.AnyAsync(
            item => item.UserId == userId
                && item.EquipmentId == request.EquipmentId
                && (item.Status == Pending
                    || item.Status == TeacherPending
                    || item.Status == Borrowed),
            cancellationToken);
        if (hasDuplicate)
        {
            return Conflict(new { message = "Bạn đã có yêu cầu hoặc phiếu mượn đang hoạt động cho tài sản này." });
        }

        var record = new BorrowRecord
        {
            UserId = userId,
            EquipmentId = request.EquipmentId,
            TeacherId = request.TeacherId,
            BorrowDate = DateTime.UtcNow,
            ExpectedReturnDate = request.ExpectedReturnDate,
            Purpose = request.Purpose,
            Status = request.TeacherId.HasValue ? TeacherPending : Pending,
            Details =
            [
                new BorrowRequestDetail
                {
                    EquipmentId = request.EquipmentId,
                    Quantity = 1,
                    Note = request.Purpose
                }
            ]
        };

        _context.BorrowRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Create",
            nameof(BorrowRecord),
            record.Id,
            new { record.EquipmentId, record.TeacherId, record.ExpectedReturnDate },
            cancellationToken);

        var message = request.TeacherId.HasValue
            ? "Có yêu cầu mượn mới đang chờ giảng viên bảo lãnh."
            : "Có yêu cầu mượn mới đang chờ quản lý lab duyệt.";
        if (request.TeacherId.HasValue)
        {
            await _hubContext.Clients.User(request.TeacherId.Value.ToString())
                .SendAsync("ReceiveNotification", message, cancellationToken);
        }
        else
        {
            await _hubContext.Clients.Group(NotificationHub.ManagerGroup)
                .SendAsync("ReceiveNotification", message, cancellationToken);
        }

        return Ok(new { record.Id, message = "Đã gửi yêu cầu mượn thành công." });
    }

    [HttpGet("pending")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult<IEnumerable<object>>> GetPendingRequests(
        CancellationToken cancellationToken)
    {
        var requests = await _context.BorrowRecords
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Equipment)
                .ThenInclude(item => item!.AssetCategory)
            .Include(item => item.Details)
                .ThenInclude(item => item.Equipment)
            .Where(item => item.Status == Pending || item.Status == Borrowed)
            .OrderByDescending(item => item.BorrowDate)
            .ToListAsync(cancellationToken);

        return Ok(requests.Select(item => new
        {
            id = item.Id,
            student = item.User!.Username,
            device = item.Equipment!.Name,
            category = item.Equipment.AssetCategory?.Name ?? string.Empty,
            serial = item.Equipment.Serial,
            requestDate = item.BorrowDate,
            returnDate = item.ExpectedReturnDate,
            purpose = item.Purpose,
            status = item.Status,
            isOverdue = item.Status == Borrowed
                && item.ExpectedReturnDate.Date < DateTime.UtcNow.Date,
            daysUntilDue = (item.ExpectedReturnDate.Date - DateTime.UtcNow.Date).Days,
            details = item.Details.Select(detail => new
            {
                detail.Id,
                detail.EquipmentId,
                equipmentName = detail.Equipment?.Name ?? string.Empty,
                detail.Quantity,
                detail.Note
            })
        }));
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<object>>> GetHistory(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var role = User.FindFirstValue(ClaimTypes.Role);

        var query = _context.BorrowRecords
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Equipment)
            .Where(item => item.Status != Pending && item.Status != TeacherPending);

        if (role is Roles.Student or Roles.Teacher)
        {
            query = query.Where(item => item.UserId == userId);
        }

        var history = await query
            .OrderByDescending(item => item.BorrowDate)
            .Select(item => new
            {
                id = item.Id,
                student = item.User!.Username,
                device = item.Equipment!.Name,
                serial = item.Equipment.Serial,
                requestDate = item.BorrowDate,
                returnDate = item.ActualReturnDate ?? item.ExpectedReturnDate,
                status = item.Status,
                returnCondition = item.ReturnCondition,
                returnInspectionNote = item.ReturnInspectionNote,
                warrantyAction = item.WarrantyAction,
                compensationAmount = item.CompensationAmount
            })
            .ToListAsync(cancellationToken);

        return Ok(history);
    }

    [HttpGet("teacher-pending")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<ActionResult<IEnumerable<object>>> GetTeacherPendingRequests(
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        var requests = await _context.BorrowRecords
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Equipment)
            .Where(item => item.Status == TeacherPending && item.TeacherId == teacherId)
            .Select(item => new
            {
                id = item.Id,
                student = item.User!.Username,
                device = item.Equipment!.Name,
                requestDate = item.BorrowDate,
                returnDate = item.ExpectedReturnDate,
                purpose = item.Purpose,
                status = item.Status
            })
            .ToListAsync(cancellationToken);

        return Ok(requests);
    }

    [HttpPut("{id:int}/teacher-approve")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<IActionResult> TeacherApproveRequest(
        int id,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        var updated = await _context.BorrowRecords
            .Where(item => item.Id == id
                && item.Status == TeacherPending
                && item.TeacherId == teacherId)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(item => item.Status, Pending),
                cancellationToken);
        if (updated == 0)
        {
            return Conflict(new { message = "Yêu cầu không tồn tại hoặc đã được xử lý." });
        }

        await _auditService.WriteAsync(
            HttpContext,
            "TeacherApprove",
            nameof(BorrowRecord),
            id,
            cancellationToken: cancellationToken);
        await _hubContext.Clients.Group(NotificationHub.ManagerGroup)
            .SendAsync("ReceiveNotification", "Có yêu cầu mượn đã được giảng viên bảo lãnh.", cancellationToken);
        return Ok(new { message = "Đã duyệt bảo lãnh." });
    }

    [HttpPut("{id:int}/teacher-reject")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<IActionResult> TeacherRejectRequest(
        int id,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        var record = await _context.BorrowRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == id && item.TeacherId == teacherId,
                cancellationToken);
        if (record is null)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu." });
        }

        var updated = await _context.BorrowRecords
            .Where(item => item.Id == id
                && item.Status == TeacherPending
                && item.TeacherId == teacherId)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(item => item.Status, Rejected),
                cancellationToken);
        if (updated == 0)
        {
            return Conflict(new { message = "Yêu cầu đã được xử lý." });
        }

        await _auditService.WriteAsync(
            HttpContext,
            "TeacherReject",
            nameof(BorrowRecord),
            id,
            cancellationToken: cancellationToken);
        await _hubContext.Clients.User(record.UserId.ToString())
            .SendAsync("ReceiveNotification", "Yêu cầu mượn của bạn đã bị từ chối bảo lãnh.", cancellationToken);
        return Ok(new { message = "Đã từ chối bảo lãnh." });
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> ApproveRequest(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await _context.BorrowRecords
            .AsNoTracking()
            .Include(item => item.Equipment)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (record is null || record.Status != Pending)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu đang chờ duyệt." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var claimedRequest = await _context.BorrowRecords
            .Where(item => item.Id == id && item.Status == Pending)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(item => item.Status, ProcessingApproval),
                cancellationToken);
        if (claimedRequest == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Yêu cầu đã được người khác xử lý." });
        }

        var claimedEquipment = await _context.Equipments
            .Where(item => item.Id == record.EquipmentId
                && item.Status == EquipmentStatuses.Available)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Status, EquipmentStatuses.Borrowed)
                    .SetProperty(item => item.BorrowCount, item => item.BorrowCount + 1),
                cancellationToken);
        if (claimedEquipment == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Tài sản này không còn sẵn sàng." });
        }

        await _context.BorrowRecords
            .Where(item => item.Id == id && item.Status == ProcessingApproval)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(item => item.Status, Borrowed),
                cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Approve",
            nameof(BorrowRecord),
            id,
            new { record.EquipmentId },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _hubContext.Clients.User(record.UserId.ToString())
            .SendAsync(
                "ReceiveNotification",
                $"Yêu cầu mượn {record.Equipment?.Name} đã được duyệt.",
                cancellationToken);
        return Ok(new { message = "Đã duyệt yêu cầu mượn." });
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> RejectRequest(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await _context.BorrowRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (record is null)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu." });
        }

        var updated = await _context.BorrowRecords
            .Where(item => item.Id == id && item.Status == Pending)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(item => item.Status, Rejected),
                cancellationToken);
        if (updated == 0)
        {
            return Conflict(new { message = "Yêu cầu đã được xử lý." });
        }

        await _auditService.WriteAsync(
            HttpContext,
            "Reject",
            nameof(BorrowRecord),
            id,
            cancellationToken: cancellationToken);
        await _hubContext.Clients.User(record.UserId.ToString())
            .SendAsync("ReceiveNotification", "Yêu cầu mượn của bạn đã bị từ chối.", cancellationToken);
        return Ok(new { message = "Đã từ chối yêu cầu mượn." });
    }

    [HttpPut("{id:int}/return")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> ReturnEquipment(
        int id,
        [FromBody] ReturnInspectionDto? dto,
        CancellationToken cancellationToken)
    {
        dto ??= new ReturnInspectionDto();
        dto.Condition = dto.Condition.Trim();
        dto.Note = dto.Note.Trim();

        if (dto.Condition is not (EquipmentStatuses.Available or EquipmentStatuses.Broken))
        {
            return BadRequest(new { message = "Tình trạng trả chỉ có thể là Rảnh hoặc Hỏng." });
        }

        if (dto.Note.Length > 2000 || dto.CompensationAmount < 0)
        {
            return BadRequest(new { message = "Ghi chú hoặc số tiền bồi thường không hợp lệ." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var claimedRequest = await _context.BorrowRecords
            .Where(item => item.Id == id && item.Status == Borrowed)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(item => item.Status, ProcessingReturn),
                cancellationToken);
        if (claimedRequest == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Phiếu mượn không tồn tại hoặc đã được xử lý." });
        }

        var record = await _context.BorrowRecords
            .Include(item => item.Equipment)
            .FirstAsync(item => item.Id == id, cancellationToken);
        var equipment = record.Equipment!;
        var isWarrantyActive = equipment.WarrantyExpiry.HasValue
            && equipment.WarrantyExpiry.Value >= DateTime.UtcNow;

        record.ActualReturnDate = DateTime.UtcNow;
        record.ReturnCondition = dto.Condition;
        record.ReturnInspectionNote = dto.Note;
        record.IsUnderWarrantyAtReturn = isWarrantyActive;
        record.InspectedByUserId = GetCurrentUserId();

        if (dto.Condition == EquipmentStatuses.Available)
        {
            record.Status = BorrowStatuses.Returned;
            record.WarrantyAction = "Không cần xử lý";
            record.CompensationAmount = 0;
            equipment.Status = EquipmentStatuses.Available;
        }
        else if (isWarrantyActive)
        {
            record.Status = BorrowStatuses.ReturnedDamaged;
            record.WarrantyAction = "Còn bảo hành - chuyển sửa/bảo hành";
            record.CompensationAmount = 0;
            equipment.Status = EquipmentStatuses.Warranty;
            AddMaintenance(record, dto.Note, "Bảo hành");
        }
        else
        {
            record.Status = BorrowStatuses.ReturnedDamaged;
            record.WarrantyAction = "Hết bảo hành - kiểm tra bồi thường";
            record.CompensationAmount = dto.CompensationAmount;
            equipment.Status = EquipmentStatuses.Broken;
            AddMaintenance(record, dto.Note, "Kiểm tra trả");

            if (dto.CompensationAmount > 0)
            {
                _context.Penalties.Add(new Penalty
                {
                    UserId = record.UserId,
                    EquipmentId = record.EquipmentId,
                    BorrowRecordId = record.Id,
                    Reason = string.IsNullOrWhiteSpace(dto.Note)
                        ? "Tài sản hỏng khi trả"
                        : dto.Note,
                    Amount = dto.CompensationAmount,
                    Status = PenaltyStatuses.Unpaid,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Return",
            nameof(BorrowRecord),
            id,
            new
            {
                dto.Condition,
                WarrantyActive = isWarrantyActive,
                dto.CompensationAmount
            },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _hubContext.Clients.User(record.UserId.ToString())
            .SendAsync("ReceiveNotification", "Phiếu trả tài sản của bạn đã được ghi nhận.", cancellationToken);
        return Ok(new { message = "Đã ghi nhận trả tài sản." });
    }

    [HttpPut("{id:int}/report-damage")]
    [Authorize(Roles = Roles.Managers)]
    public Task<IActionResult> ReportDamage(
        int id,
        [FromBody] ReportDamageDto dto,
        CancellationToken cancellationToken)
    {
        return ReturnEquipment(
            id,
            new ReturnInspectionDto
            {
                Condition = EquipmentStatuses.Broken,
                Note = dto.Reason,
                CompensationAmount = dto.Amount
            },
            cancellationToken);
    }

    [HttpPost("{id:int}/remind")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> RemindReturn(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await _context.BorrowRecords
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Equipment)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (record is null || record.Status != Borrowed)
        {
            return NotFound(new { message = "Không tìm thấy phiếu mượn hợp lệ." });
        }

        if (string.IsNullOrWhiteSpace(record.User?.Email))
        {
            return BadRequest(new { message = "Người mượn chưa cập nhật địa chỉ email." });
        }

        var username = WebUtility.HtmlEncode(record.User.Username);
        var equipmentName = WebUtility.HtmlEncode(record.Equipment?.Name ?? "tài sản");
        var subject = $"[Lab] Nhắc trả tài sản: {record.Equipment?.Name}";
        var body = $"""
            <h3>Chào {username},</h3>
            <p>Bạn đang mượn tài sản <strong>{equipmentName}</strong> từ phòng lab.</p>
            <p>Hạn trả dự kiến: <strong>{record.ExpectedReturnDate:dd/MM/yyyy}</strong>.</p>
            <p>Vui lòng hoàn trả đúng hạn để bộ phận lab kiểm tra tình trạng tài sản.</p>
            <p>Trân trọng,<br/>Lab Management</p>
            """;

        await _emailService.SendEmailAsync(
            record.User.Email,
            subject,
            body,
            cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "SendReturnReminder",
            nameof(BorrowRecord),
            id,
            cancellationToken: cancellationToken);
        return Ok(new { message = "Đã gửi email nhắc trả thành công." });
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private void AddMaintenance(BorrowRecord record, string note, string performedBy)
    {
        _context.MaintenanceRecords.Add(new MaintenanceRecord
        {
            EquipmentId = record.EquipmentId,
            MaintenanceDate = DateTime.UtcNow,
            Description = string.IsNullOrWhiteSpace(note)
                ? "Kiểm tra tài sản sau khi trả."
                : note,
            Cost = 0,
            PerformedBy = performedBy
        });
    }
}
