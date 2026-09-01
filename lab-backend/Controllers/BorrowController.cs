using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using LabManagementAPI.Data;
using LabManagementAPI.Dtos;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BorrowController : ControllerBase
{
    private const string Pending = BorrowStatuses.Pending;
    private const string TeacherPending = BorrowStatuses.TeacherPending;
    private const string Approved = BorrowStatuses.Approved;
    private const string Borrowed = BorrowStatuses.Borrowed;
    private const string Rejected = BorrowStatuses.Rejected;
    private const string ProcessingApproval = BorrowStatuses.ProcessingApproval;
    private const string ProcessingReturn = BorrowStatuses.ReturnProcessing;

    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly IFileStorage _fileStorage;
    private readonly IConfiguration _configuration;

    public BorrowController(
        AppDbContext context,
        IEmailService emailService,
        INotificationService notificationService,
        IAuditService auditService,
        IFileStorage fileStorage,
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _notificationService = notificationService;
        _auditService = auditService;
        _fileStorage = fileStorage;
        _configuration = configuration;
    }

    public sealed class BorrowRequestDto
    {
        // EquipmentId remains for clients from the previous one-item API.
        public int? EquipmentId { get; set; }
        public List<BorrowItemDto> Items { get; set; } = new();
        public DateTime ExpectedReturnDate { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public int? TeacherId { get; set; }
    }

    public sealed class BorrowItemDto
    {
        public int EquipmentId { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public sealed class DecisionNoteDto
    {
        public string Note { get; set; } = string.Empty;
    }

    public sealed class ReturnInspectionDto
    {
        public string Condition { get; set; } = EquipmentStatuses.Available;
        public string Note { get; set; } = string.Empty;
        public decimal CompensationAmount { get; set; }
        public List<ReturnItemDto> Items { get; set; } = new();
    }

    public sealed class ReturnItemDto
    {
        public int EquipmentId { get; set; }
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
        request.Items ??= new();

        if (request.EquipmentId.HasValue && request.EquipmentId.Value > 0 && request.Items.Count == 0)
        {
            request.Items.Add(new BorrowItemDto { EquipmentId = request.EquipmentId.Value });
        }

        var requestedItems = request.Items
            .Where(item => item.EquipmentId > 0)
            .GroupBy(item => item.EquipmentId)
            .Select(group => group.First())
            .ToList();
        if (requestedItems.Count == 0)
        {
            return BadRequest(new { message = "Phiếu mượn phải có ít nhất một tài sản hợp lệ." });
        }

        if (VietnamTime.Date(request.ExpectedReturnDate) <= VietnamTime.Today())
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

        var equipmentIds = requestedItems.Select(item => item.EquipmentId).ToArray();
        var equipments = await _context.Equipments
            .AsNoTracking()
            .Where(item => equipmentIds.Contains(item.Id)
                && item.Status == EquipmentStatuses.Available)
            .ToListAsync(cancellationToken);
        if (equipments.Count != equipmentIds.Length)
        {
            return BadRequest(new { message = "Một hoặc nhiều tài sản không tồn tại hoặc không sẵn sàng để mượn." });
        }

        var hasDuplicate = await _context.BorrowRecords.AnyAsync(
            item => item.UserId == userId
                && (item.Status == Pending
                    || item.Status == TeacherPending
                    || item.Status == Approved
                    || item.Status == ProcessingApproval
                    || item.Status == Borrowed
                    || item.Status == ProcessingReturn)
                && item.Details.Any(detail => equipmentIds.Contains(detail.EquipmentId)),
            cancellationToken);
        if (hasDuplicate)
        {
            return Conflict(new { message = "Bạn đã có yêu cầu hoặc phiếu mượn đang hoạt động cho tài sản này." });
        }

        var initialStatus = request.TeacherId.HasValue ? TeacherPending : Pending;
        var record = new BorrowRecord
        {
            UserId = userId,
            EquipmentId = null,
            TeacherId = request.TeacherId,
            BorrowDate = DateTime.UtcNow,
            ExpectedReturnDate = request.ExpectedReturnDate,
            Purpose = request.Purpose,
            Status = initialStatus,
            Details = requestedItems.Select(item => new BorrowRequestDetail
            {
                EquipmentId = item.EquipmentId,
                Quantity = 1,
                Note = string.IsNullOrWhiteSpace(item.Note) ? request.Purpose : item.Note.Trim(),
                Status = initialStatus
            }).ToList(),
            StatusHistory =
            [
                new BorrowStatusHistory
                {
                    ToStatus = initialStatus,
                    Note = "Tạo phiếu mượn",
                    ChangedByUserId = userId
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
            new { EquipmentIds = equipmentIds, record.TeacherId, record.ExpectedReturnDate },
            cancellationToken);

        var message = request.TeacherId.HasValue
            ? "Có yêu cầu mượn mới đang chờ giảng viên bảo lãnh."
            : "Có yêu cầu mượn mới đang chờ quản lý lab duyệt.";
        if (request.TeacherId.HasValue)
        {
            await _notificationService.NotifyUserAsync(
                request.TeacherId.Value,
                "BORROW_TEACHER_PENDING",
                "Yêu cầu mượn cần bảo lãnh",
                message,
                "/dashboard/teacher-approval",
                cancellationToken);
        }
        else
        {
            await _notificationService.NotifyManagersAsync(
                "BORROW_PENDING",
                "Yêu cầu mượn mới",
                message,
                "/dashboard/borrow-requests",
                cancellationToken);
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
        .Where(item => item.Status == Pending
            || item.Status == Approved
            || item.Status == Borrowed
            || item.Status == ProcessingReturn)
            .OrderByDescending(item => item.BorrowDate)
            .ToListAsync(cancellationToken);

        var requestIds = requests.Select(item => item.Id).ToArray();
        var handovers = await _context.HandoverRecords
            .AsNoTracking()
            .Where(item => requestIds.Contains(item.BorrowRecordId))
            .ToDictionaryAsync(item => item.BorrowRecordId, cancellationToken);

        return Ok(requests.Select(item => new
        {
            id = item.Id,
            student = item.User!.Username,
            device = item.Equipment?.Name ?? $"Nhiều tài sản ({item.Details.Count})",
            equipmentId = item.EquipmentId,
            category = item.Equipment?.AssetCategory?.Name ?? string.Empty,
            serial = item.Equipment?.Serial ?? string.Empty,
            assetCode = item.Equipment?.AssetCode ?? string.Empty,
            borrowerName = item.User!.FullName,
            requestDate = item.BorrowDate,
            returnDate = item.ExpectedReturnDate,
            purpose = item.Purpose,
            status = item.Status,
            hasHandover = handovers.ContainsKey(item.Id),
            handoverCode = handovers.GetValueOrDefault(item.Id)?.Code,
            handoverConfirmedAt = handovers.GetValueOrDefault(item.Id)?.ConfirmedAt,
            isOverdue = item.Status == Borrowed
                && VietnamTime.Date(item.ExpectedReturnDate) < VietnamTime.Today(),
            daysUntilDue = (VietnamTime.Date(item.ExpectedReturnDate) - VietnamTime.Today()).Days,
            details = item.Details.Select(detail => new
            {
                detail.Id,
                detail.EquipmentId,
                equipmentName = detail.Equipment?.Name ?? string.Empty,
                serial = detail.Equipment?.Serial ?? string.Empty,
                assetCode = detail.Equipment?.AssetCode ?? string.Empty,
                detail.Quantity,
                detail.Note,
                detail.Status,
                detail.ReturnCondition,
                detail.ReturnNote,
                detail.ReturnedAt
            })
        }));
    }

    [HttpGet("pending/paged")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> GetPendingRequestsPaged(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        var query = _context.BorrowRecords
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Equipment)
                .ThenInclude(item => item!.AssetCategory)
            .Include(item => item.Details)
                .ThenInclude(item => item.Equipment)
            .Where(item => item.Status == Pending
                || item.Status == Approved
                || item.Status == Borrowed
                || item.Status == ProcessingReturn)
            .AsQueryable();
        var search = paging.NormalizedSearch;
        if (search.Length > 0)
        {
            query = query.Where(item =>
                item.User!.Username.Contains(search)
                || item.User.FullName.Contains(search)
                || item.Purpose.Contains(search)
                || (item.Equipment != null
                    && (item.Equipment.Name.Contains(search)
                        || item.Equipment.Serial.Contains(search)
                        || item.Equipment.AssetCode.Contains(search)))
                || item.Details.Any(detail => detail.Equipment != null
                    && (detail.Equipment.Name.Contains(search)
                        || detail.Equipment.Serial.Contains(search)
                        || detail.Equipment.AssetCode.Contains(search))));
        }
        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            var status = paging.Status.Trim();
            query = query.Where(item => item.Status == status);
        }
        if (paging.From.HasValue)
        {
            query = query.Where(item => item.BorrowDate >= paging.From.Value);
        }
        if (paging.To.HasValue)
        {
            var exclusiveTo = paging.To.Value.Date.AddDays(1);
            query = query.Where(item => item.BorrowDate < exclusiveTo);
        }

        var page = await query
            .OrderByDescending(item => item.BorrowDate)
            .ThenByDescending(item => item.Id)
            .ToPagedResultAsync(paging, cancellationToken);
        var requestIds = page.Items.Select(item => item.Id).ToArray();
        var handovers = await _context.HandoverRecords
            .AsNoTracking()
            .Where(item => requestIds.Contains(item.BorrowRecordId))
            .ToDictionaryAsync(item => item.BorrowRecordId, cancellationToken);
        var today = VietnamTime.Today();
        var items = page.Items.Select(item => (object)new
        {
            id = item.Id,
            student = item.User!.Username,
            device = item.Equipment?.Name ?? $"Nhiều tài sản ({item.Details.Count})",
            equipmentId = item.EquipmentId,
            category = item.Equipment?.AssetCategory?.Name ?? string.Empty,
            serial = item.Equipment?.Serial ?? string.Empty,
            assetCode = item.Equipment?.AssetCode ?? string.Empty,
            borrowerName = item.User.FullName,
            requestDate = item.BorrowDate,
            returnDate = item.ExpectedReturnDate,
            purpose = item.Purpose,
            status = item.Status,
            hasHandover = handovers.ContainsKey(item.Id),
            handoverCode = handovers.GetValueOrDefault(item.Id)?.Code,
            handoverConfirmedAt = handovers.GetValueOrDefault(item.Id)?.ConfirmedAt,
            isOverdue = item.Status == Borrowed && VietnamTime.Date(item.ExpectedReturnDate) < today,
            daysUntilDue = (VietnamTime.Date(item.ExpectedReturnDate) - today).Days,
            details = item.Details.Select(detail => new
            {
                detail.Id,
                detail.EquipmentId,
                equipmentName = detail.Equipment?.Name ?? string.Empty,
                serial = detail.Equipment?.Serial ?? string.Empty,
                assetCode = detail.Equipment?.AssetCode ?? string.Empty,
                detail.Quantity,
                detail.Note,
                detail.Status,
                detail.ReturnCondition,
                detail.ReturnNote,
                detail.ReturnedAt
            })
        }).ToList();

        return Ok(new PagedResult<object>(items, page.Total, page.Page, page.PageSize, page.TotalPages));
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
            .Include(item => item.Details)
                .ThenInclude(detail => detail.Equipment)
            .Include(item => item.StatusHistory)
            .Where(item => item.Status != Pending && item.Status != TeacherPending);

        if (role is Roles.Student or Roles.Teacher)
        {
            query = query.Where(item => item.UserId == userId);
        }

        var records = await query
            .OrderByDescending(item => item.BorrowDate)
            .ToListAsync(cancellationToken);
        var recordIds = records.Select(item => item.Id).ToArray();
        var handovers = await _context.HandoverRecords
            .AsNoTracking()
            .Where(item => recordIds.Contains(item.BorrowRecordId))
            .Include(item => item.Items)
                .ThenInclude(item => item.Equipment)
            .ToDictionaryAsync(item => item.BorrowRecordId, cancellationToken);

        var history = records.Select(item =>
        {
            handovers.TryGetValue(item.Id, out var handover);
            return new
            {
                id = item.Id,
                student = item.User!.Username,
                device = item.Equipment != null ? item.Equipment.Name : $"Nhiều tài sản ({item.Details.Count})",
                serial = item.Equipment != null ? item.Equipment.Serial : string.Empty,
                requestDate = item.BorrowDate,
                returnDate = item.ActualReturnDate ?? item.ExpectedReturnDate,
                status = item.Status,
                returnCondition = item.ReturnCondition,
                returnInspectionNote = item.ReturnInspectionNote,
                warrantyAction = item.WarrantyAction,
                compensationAmount = item.CompensationAmount,
                handover = handover is null ? null : new
                {
                    handover.Id,
                    handover.Code,
                    handover.HandoverAt,
                    handover.Notes,
                    handover.ConfirmedAt,
                    items = handover.Items.Select(handoverItem => new
                    {
                        handoverItem.EquipmentId,
                        equipmentName = handoverItem.Equipment?.Name ?? string.Empty,
                        serial = handoverItem.Equipment?.Serial ?? string.Empty,
                        handoverItem.Condition,
                        handoverItem.Accessories,
                        handoverItem.Note
                    })
                },
                canConfirmHandover = item.UserId == userId
                    && item.Status == Approved
                    && handover is not null
                    && handover.ConfirmedAt is null,
                details = item.Details.Select(detail => new
                {
                    detail.Id,
                    detail.EquipmentId,
                    equipmentName = detail.Equipment!.Name,
                    serial = detail.Equipment.Serial,
                    detail.Quantity,
                    detail.Status,
                    detail.ReturnCondition,
                    detail.ReturnNote,
                    detail.ReturnedAt,
                    detail.CompensationAmount
                }),
                statusHistory = item.StatusHistory
                    .OrderBy(history => history.CreatedAt)
                    .Select(history => new
                    {
                        history.FromStatus,
                        history.ToStatus,
                        history.Note,
                        history.CreatedAt
                    })
            };
        }).ToList();

        return Ok(history);
    }

    [HttpGet("history/paged")]
    public async Task<IActionResult> GetHistoryPaged(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var role = User.FindFirstValue(ClaimTypes.Role);
        var query = _context.BorrowRecords
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Equipment)
            .Include(item => item.Details)
                .ThenInclude(detail => detail.Equipment)
            .Include(item => item.StatusHistory)
            .Where(item => item.Status != Pending && item.Status != TeacherPending)
            .AsQueryable();
        if (role is Roles.Student or Roles.Teacher)
        {
            query = query.Where(item => item.UserId == userId);
        }

        var search = paging.NormalizedSearch;
        if (search.Length > 0)
        {
            query = query.Where(item =>
                item.User!.Username.Contains(search)
                || item.User.FullName.Contains(search)
                || item.Purpose.Contains(search)
                || (item.Equipment != null
                    && (item.Equipment.Name.Contains(search) || item.Equipment.Serial.Contains(search)))
                || item.Details.Any(detail => detail.Equipment != null
                    && (detail.Equipment.Name.Contains(search) || detail.Equipment.Serial.Contains(search))));
        }
        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            var status = paging.Status.Trim();
            query = query.Where(item => item.Status == status);
        }
        if (paging.From.HasValue)
        {
            query = query.Where(item => item.BorrowDate >= paging.From.Value);
        }
        if (paging.To.HasValue)
        {
            var exclusiveTo = paging.To.Value.Date.AddDays(1);
            query = query.Where(item => item.BorrowDate < exclusiveTo);
        }

        var page = await query
            .OrderByDescending(item => item.BorrowDate)
            .ThenByDescending(item => item.Id)
            .ToPagedResultAsync(paging, cancellationToken);
        var recordIds = page.Items.Select(item => item.Id).ToArray();
        var handovers = await _context.HandoverRecords
            .AsNoTracking()
            .Where(item => recordIds.Contains(item.BorrowRecordId))
            .Include(item => item.Items)
                .ThenInclude(item => item.Equipment)
            .ToDictionaryAsync(item => item.BorrowRecordId, cancellationToken);
        var items = page.Items.Select(item =>
        {
            handovers.TryGetValue(item.Id, out var handover);
            return (object)new
            {
                id = item.Id,
                student = item.User!.Username,
                device = item.Equipment?.Name ?? $"Nhiều tài sản ({item.Details.Count})",
                serial = item.Equipment?.Serial ?? string.Empty,
                requestDate = item.BorrowDate,
                returnDate = item.ActualReturnDate ?? item.ExpectedReturnDate,
                status = item.Status,
                returnCondition = item.ReturnCondition,
                returnInspectionNote = item.ReturnInspectionNote,
                warrantyAction = item.WarrantyAction,
                compensationAmount = item.CompensationAmount,
                handover = handover is null ? null : new
                {
                    handover.Id,
                    handover.Code,
                    handover.HandoverAt,
                    handover.Notes,
                    handover.ConfirmedAt,
                    items = handover.Items.Select(handoverItem => new
                    {
                        handoverItem.EquipmentId,
                        equipmentName = handoverItem.Equipment?.Name ?? string.Empty,
                        serial = handoverItem.Equipment?.Serial ?? string.Empty,
                        handoverItem.Condition,
                        handoverItem.Accessories,
                        handoverItem.Note
                    })
                },
                canConfirmHandover = item.UserId == userId
                    && item.Status == Approved
                    && handover is not null
                    && handover.ConfirmedAt is null,
                details = item.Details.Select(detail => new
                {
                    detail.Id,
                    detail.EquipmentId,
                    equipmentName = detail.Equipment!.Name,
                    serial = detail.Equipment.Serial,
                    detail.Quantity,
                    detail.Status,
                    detail.ReturnCondition,
                    detail.ReturnNote,
                    detail.ReturnedAt,
                    detail.CompensationAmount
                }),
                statusHistory = item.StatusHistory
                    .OrderBy(history => history.CreatedAt)
                    .Select(history => new
                    {
                        history.FromStatus,
                        history.ToStatus,
                        history.Note,
                        history.CreatedAt
                    })
            };
        }).ToList();

        return Ok(new PagedResult<object>(items, page.Total, page.Page, page.PageSize, page.TotalPages));
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
            .Include(item => item.Details)
                .ThenInclude(detail => detail.Equipment)
            .Where(item => item.Status == TeacherPending && item.TeacherId == teacherId)
            .Select(item => new
            {
                id = item.Id,
                student = item.User!.Username,
                device = item.Equipment != null ? item.Equipment.Name : $"Nhiều tài sản ({item.Details.Count})",
                requestDate = item.BorrowDate,
                returnDate = item.ExpectedReturnDate,
                purpose = item.Purpose,
                status = item.Status,
                details = item.Details.Select(detail => new
                {
                    detail.EquipmentId,
                    equipmentName = detail.Equipment!.Name,
                    serial = detail.Equipment.Serial,
                    detail.Note,
                    detail.Status
                })
            })
            .ToListAsync(cancellationToken);

        return Ok(requests);
    }

    [HttpGet("teacher-pending/paged")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<IActionResult> GetTeacherPendingRequestsPaged(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        var teacherId = GetCurrentUserId();
        var query = _context.BorrowRecords
            .AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Equipment)
            .Include(item => item.Details)
                .ThenInclude(detail => detail.Equipment)
            .Where(item => item.Status == TeacherPending && item.TeacherId == teacherId)
            .AsQueryable();
        var search = paging.NormalizedSearch;
        if (search.Length > 0)
        {
            query = query.Where(item =>
                item.User!.Username.Contains(search)
                || item.User.FullName.Contains(search)
                || item.Purpose.Contains(search)
                || (item.Equipment != null && item.Equipment.Name.Contains(search))
                || item.Details.Any(detail => detail.Equipment != null && detail.Equipment.Name.Contains(search)));
        }

        var page = await query
            .OrderByDescending(item => item.BorrowDate)
            .ThenByDescending(item => item.Id)
            .ToPagedResultAsync(paging, cancellationToken);
        var items = page.Items.Select(item => (object)new
        {
            id = item.Id,
            student = item.User!.Username,
            device = item.Equipment?.Name ?? $"Nhiều tài sản ({item.Details.Count})",
            requestDate = item.BorrowDate,
            returnDate = item.ExpectedReturnDate,
            purpose = item.Purpose,
            status = item.Status,
            details = item.Details.Select(detail => new
            {
                detail.EquipmentId,
                equipmentName = detail.Equipment!.Name,
                serial = detail.Equipment.Serial,
                detail.Note,
                detail.Status
            })
        }).ToList();
        return Ok(new PagedResult<object>(items, page.Total, page.Page, page.PageSize, page.TotalPages));
    }

    [HttpPut("{id:int}/teacher-approve")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<IActionResult> TeacherApproveRequest(
        int id,
        [FromBody] DecisionNoteDto dto,
        CancellationToken cancellationToken)
    {
        var note = NormalizeDecisionNote(dto, required: true);
        if (note is null)
        {
            return BadRequest(new { message = "Giảng viên phải nhập ghi chú khi duyệt." });
        }
        var teacherId = GetCurrentUserId();
        var updated = await _context.BorrowRecords
            .Where(item => item.Id == id
                && item.Status == TeacherPending
                && item.TeacherId == teacherId)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Status, Pending)
                    .SetProperty(item => item.TeacherDecisionNote, note),
                cancellationToken);
        if (updated == 0)
        {
            return Conflict(new { message = "Yêu cầu không tồn tại hoặc đã được xử lý." });
        }

        await _context.BorrowRequestDetails
            .Where(detail => detail.BorrowRecordId == id)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(detail => detail.Status, Pending),
                cancellationToken);

        _context.BorrowStatusHistories.Add(new BorrowStatusHistory
        {
            BorrowRecordId = id,
            FromStatus = TeacherPending,
            ToStatus = Pending,
            Note = note,
            ChangedByUserId = teacherId
        });

        await _auditService.WriteAsync(
            HttpContext,
            "TeacherApprove",
            nameof(BorrowRecord),
            id,
            cancellationToken: cancellationToken);
        await _notificationService.NotifyManagersAsync(
            "BORROW_TEACHER_APPROVED",
            "Yêu cầu mượn đã được bảo lãnh",
            "Có yêu cầu mượn đã được giảng viên bảo lãnh.",
            "/dashboard/borrow-requests",
            cancellationToken);
        return Ok(new { message = "Đã duyệt bảo lãnh." });
    }

    [HttpPut("{id:int}/teacher-reject")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<IActionResult> TeacherRejectRequest(
        int id,
        [FromBody] DecisionNoteDto dto,
        CancellationToken cancellationToken)
    {
        var note = NormalizeDecisionNote(dto, required: true);
        if (note is null)
        {
            return BadRequest(new { message = "Giảng viên phải nhập lý do từ chối." });
        }
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
                updates => updates
                    .SetProperty(item => item.Status, Rejected)
                    .SetProperty(item => item.TeacherDecisionNote, note),
                cancellationToken);
        if (updated == 0)
        {
            return Conflict(new { message = "Yêu cầu đã được xử lý." });
        }

        await _context.BorrowRequestDetails
            .Where(detail => detail.BorrowRecordId == id)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(detail => detail.Status, Rejected),
                cancellationToken);

        _context.BorrowStatusHistories.Add(new BorrowStatusHistory
        {
            BorrowRecordId = id,
            FromStatus = TeacherPending,
            ToStatus = Rejected,
            Note = note,
            ChangedByUserId = teacherId
        });

        await _auditService.WriteAsync(
            HttpContext,
            "TeacherReject",
            nameof(BorrowRecord),
            id,
            cancellationToken: cancellationToken);
        await _notificationService.NotifyUserAsync(
            record.UserId,
            "BORROW_TEACHER_REJECTED",
            "Yêu cầu mượn bị từ chối bảo lãnh",
            "Yêu cầu mượn của bạn đã bị từ chối bảo lãnh.",
            "/dashboard/borrow-history",
            cancellationToken);
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
            .Include(item => item.Details)
                .ThenInclude(detail => detail.Equipment)
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

        var equipmentIds = record.Details.Select(detail => detail.EquipmentId).Distinct().ToArray();
        var claimedEquipment = await _context.Equipments
            .Where(item => equipmentIds.Contains(item.Id)
                && item.Status == EquipmentStatuses.Available)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Status, EquipmentStatuses.BorrowPending),
                cancellationToken);
        if (claimedEquipment != equipmentIds.Length)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Tài sản này không còn sẵn sàng." });
        }

        await _context.BorrowRequestDetails
            .Where(detail => detail.BorrowRecordId == id)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(detail => detail.Status, Approved),
                cancellationToken);
        await _context.BorrowRecords
            .Where(item => item.Id == id && item.Status == ProcessingApproval)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(item => item.Status, Approved),
                cancellationToken);
        _context.BorrowStatusHistories.Add(new BorrowStatusHistory
        {
            BorrowRecordId = id,
            FromStatus = ProcessingApproval,
            ToStatus = Approved,
            Note = "Quản lý lab đã duyệt; tài sản được giữ chỗ để lập biên bản bàn giao.",
            ChangedByUserId = GetCurrentUserId()
        });
        await _auditService.WriteAsync(
            HttpContext,
            "Approve",
            nameof(BorrowRecord),
            id,
            new { EquipmentIds = equipmentIds },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _notificationService.NotifyUserAsync(
            record.UserId,
            "BORROW_APPROVED",
            "Yêu cầu mượn đã được duyệt",
            $"Yêu cầu mượn {equipmentIds.Length} tài sản đã được duyệt. Vui lòng chờ quản lý lập biên bản bàn giao.",
            "/dashboard/borrow-history",
            cancellationToken);
        return Ok(new { message = "Đã duyệt và giữ chỗ tài sản. Bước tiếp theo là lập biên bản bàn giao." });
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> RejectRequest(
        int id,
        [FromBody] DecisionNoteDto? dto,
        CancellationToken cancellationToken)
    {
        var record = await _context.BorrowRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (record is null)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu." });
        }

        var note = NormalizeDecisionNote(dto, required: false) ?? "Quản lý lab từ chối yêu cầu.";
        var updated = await _context.BorrowRecords
            .Where(item => item.Id == id && item.Status == Pending)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Status, Rejected)
                    .SetProperty(item => item.ManagerDecisionNote, note),
                cancellationToken);
        if (updated == 0)
        {
            return Conflict(new { message = "Yêu cầu đã được xử lý." });
        }

        await _context.BorrowRequestDetails
            .Where(detail => detail.BorrowRecordId == id)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(detail => detail.Status, Rejected),
                cancellationToken);

        _context.BorrowStatusHistories.Add(new BorrowStatusHistory
        {
            BorrowRecordId = id,
            FromStatus = Pending,
            ToStatus = Rejected,
            Note = note,
            ChangedByUserId = GetCurrentUserId()
        });

        await _auditService.WriteAsync(
            HttpContext,
            "Reject",
            nameof(BorrowRecord),
            id,
            cancellationToken: cancellationToken);
        await _notificationService.NotifyUserAsync(
            record.UserId,
            "BORROW_REJECTED",
            "Yêu cầu mượn bị từ chối",
            "Yêu cầu mượn của bạn đã bị từ chối.",
            "/dashboard/borrow-history",
            cancellationToken);
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
        dto.Condition = NormalizeReturnCondition(dto.Condition);
        dto.Note = dto.Note.Trim();
        dto.Items ??= new();

        if (dto.Items.Count == 0)
        {
            dto.Items.Add(new ReturnItemDto
            {
                Condition = dto.Condition,
                Note = dto.Note,
                CompensationAmount = dto.CompensationAmount
            });
        }

        foreach (var item in dto.Items)
        {
            item.Condition = NormalizeReturnCondition(item.Condition);
            item.Note = item.Note.Trim();
            if (item.Condition is not (EquipmentStatuses.Available or EquipmentStatuses.Broken)
                || item.Note.Length > 2000
                || item.CompensationAmount < 0)
            {
                return BadRequest(new { message = "Tình trạng trả hoặc thông tin kiểm tra không hợp lệ." });
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var claimedRequest = await _context.BorrowRecords
            .Where(item => item.Id == id && (item.Status == Borrowed || item.Status == ProcessingReturn))
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
            .Include(item => item.Details)
                .ThenInclude(detail => detail.Equipment)
            .FirstAsync(item => item.Id == id, cancellationToken);
        var targetDetails = dto.Items.Any(item => item.EquipmentId > 0)
            ? record.Details.Where(detail => dto.Items.Select(item => item.EquipmentId).Contains(detail.EquipmentId)).ToList()
            : record.Details.ToList();
        if (targetDetails.Count == 0 || targetDetails.Any(detail => detail.ReturnedAt.HasValue))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Tài sản trong phiếu không tồn tại hoặc đã được nhận trả." });
        }

        var inspectorId = GetCurrentUserId();
        foreach (var detail in targetDetails)
        {
            var itemDto = dto.Items.FirstOrDefault(item => item.EquipmentId == detail.EquipmentId)
                ?? dto.Items[0];
            var equipment = detail.Equipment!;
            var isWarrantyActive = equipment.WarrantyExpiry.HasValue
                && equipment.WarrantyExpiry.Value >= DateTime.UtcNow;
            var note = itemDto.Note;
            detail.ReturnCondition = itemDto.Condition;
            detail.ReturnNote = note;
            detail.ReturnedAt = DateTime.UtcNow;
            detail.CompensationAmount = 0;
            detail.Status = itemDto.Condition == EquipmentStatuses.Available
                ? BorrowStatuses.Returned
                : BorrowStatuses.ReturnedDamaged;

            if (itemDto.Condition == EquipmentStatuses.Available)
            {
                equipment.Status = EquipmentStatuses.Available;
            }
            else if (isWarrantyActive)
            {
                equipment.Status = EquipmentStatuses.UnderWarranty;
                AddMaintenance(detail.EquipmentId, note, "Bảo hành");
            }
            else
            {
                equipment.Status = EquipmentStatuses.Broken;
                AddMaintenance(detail.EquipmentId, note, "Kiểm tra trả");
                detail.CompensationAmount = itemDto.CompensationAmount;
                if (itemDto.CompensationAmount > 0)
                {
                    _context.Penalties.Add(new Penalty
                    {
                        UserId = record.UserId,
                        EquipmentId = detail.EquipmentId,
                        BorrowRecordId = record.Id,
                        Reason = string.IsNullOrWhiteSpace(note) ? "Tài sản hỏng khi trả" : note,
                        Amount = itemDto.CompensationAmount,
                        Status = PenaltyStatuses.Unpaid,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        var allReturned = record.Details.All(detail => detail.ReturnedAt.HasValue);
        var anyDamaged = record.Details.Any(detail => detail.Status == BorrowStatuses.ReturnedDamaged);
        var previousStatus = record.Status;
        record.Status = allReturned
            ? (anyDamaged ? BorrowStatuses.ReturnedDamaged : BorrowStatuses.Returned)
            : ProcessingReturn;
        record.InspectedByUserId = inspectorId;
        record.ReturnCondition = targetDetails.First().ReturnCondition;
        record.ReturnInspectionNote = targetDetails.First().ReturnNote;
        record.CompensationAmount = record.Details.Sum(detail => detail.CompensationAmount);
        record.ActualReturnDate = allReturned ? DateTime.UtcNow : null;
        record.IsUnderWarrantyAtReturn = targetDetails.Any(detail =>
            detail.ReturnCondition == EquipmentStatuses.Broken
            && detail.Equipment!.WarrantyExpiry.HasValue
            && detail.Equipment.WarrantyExpiry.Value >= DateTime.UtcNow);
        record.WarrantyAction = anyDamaged ? "Đã chuyển xử lý hư hỏng/bảo hành" : "Không cần xử lý";
        _context.BorrowStatusHistories.Add(new BorrowStatusHistory
        {
            BorrowRecordId = id,
            FromStatus = previousStatus,
            ToStatus = record.Status,
            Note = allReturned ? "Đã nhận trả toàn bộ tài sản trong phiếu." : "Đã nhận trả một phần tài sản trong phiếu.",
            ChangedByUserId = inspectorId
        });

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Return",
            nameof(BorrowRecord),
            id,
            new { ItemCount = targetDetails.Count, AllReturned = allReturned },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _notificationService.NotifyUserAsync(
            record.UserId,
            "BORROW_RETURNED",
            "Đã ghi nhận nhận trả tài sản",
            "Kết quả nhận trả tài sản trong phiếu của bạn đã được ghi nhận.",
            "/dashboard/borrow-history",
            cancellationToken);
        return Ok(new { message = allReturned ? "Đã ghi nhận trả toàn bộ tài sản." : "Đã ghi nhận trả một phần tài sản." });
    }

    public sealed class UploadReturnEvidenceDto
    {
        [Required] public IFormFile? File { get; set; }
        public int? EquipmentId { get; set; }
        [Required, MaxLength(50)] public string EvidenceType { get; set; } = "PHOTO_AFTER";
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

    [HttpPost("{id:int}/return-evidence")]
    [EnableRateLimiting("sensitive")]
    [Authorize(Roles = Roles.Managers)]
    [RequestSizeLimit(11_000_000)]
    public async Task<IActionResult> UploadReturnEvidence(
        int id,
        [FromForm] UploadReturnEvidenceDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.File is null) return BadRequest(new { message = "Vui lòng chọn file minh chứng nhận trả." });
        dto.EvidenceType = dto.EvidenceType.Trim().ToUpperInvariant();
        if (dto.EvidenceType is not ("PHOTO_BEFORE" or "PHOTO_AFTER" or "DOCUMENT" or "SIGNATURE"))
            return BadRequest(new { message = "Loại minh chứng nhận trả không hợp lệ." });

        var record = await _context.BorrowRecords.AsNoTracking()
            .Include(item => item.Details)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (record is null) return NotFound(new { message = "Không tìm thấy phiếu mượn." });
        if (dto.EquipmentId.HasValue && !record.Details.Any(item => item.EquipmentId == dto.EquipmentId.Value))
            return BadRequest(new { message = "Tài sản không thuộc phiếu mượn." });

        StoredFile stored;
        try
        {
            stored = await _fileStorage.SaveAsync(
                dto.File,
                "returns",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".doc", ".docx" },
                _configuration.GetValue("Uploads:MaxEvidenceFileBytes", 10 * 1024 * 1024L),
                cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        var evidence = new ReturnEvidence
        {
            BorrowRecordId = id,
            EquipmentId = dto.EquipmentId,
            EvidenceType = dto.EvidenceType,
            OriginalFileName = stored.OriginalFileName,
            StoredPath = stored.StoredPath,
            ContentType = stored.ContentType,
            FileSize = stored.Length,
            UploadedByUserId = GetCurrentUserId()
        };
        _context.ReturnEvidence.Add(evidence);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "UploadEvidence", nameof(BorrowRecord), id,
            new { evidence.Id, evidence.EquipmentId, evidence.EvidenceType }, cancellationToken);
        return Ok(new { evidence.Id, evidence.OriginalFileName, message = "Đã lưu minh chứng nhận trả." });
    }

    [HttpGet("{id:int}/return-evidence/{evidenceId:long}")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> DownloadReturnEvidence(
        int id,
        long evidenceId,
        CancellationToken cancellationToken)
    {
        var evidence = await _context.ReturnEvidence.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == evidenceId && item.BorrowRecordId == id, cancellationToken);
        if (evidence is null) return NotFound();
        var stream = await _fileStorage.OpenReadAsync(evidence.StoredPath, cancellationToken);
        if (stream is null) return NotFound();
        return File(stream, evidence.ContentType, evidence.OriginalFileName, enableRangeProcessing: true);
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private static string? NormalizeDecisionNote(DecisionNoteDto? dto, bool required)
    {
        var note = dto?.Note?.Trim();
        if (string.IsNullOrWhiteSpace(note))
        {
            return required ? null : string.Empty;
        }

        return note.Length > 2000 ? note[..2000] : note;
    }

    private static string NormalizeReturnCondition(string? condition)
    {
        return condition?.Trim() switch
        {
            "Rảnh" or "Sẵn sàng" => EquipmentStatuses.Available,
            "Hỏng" => EquipmentStatuses.Broken,
            _ => condition?.Trim() ?? string.Empty
        };
    }

    private void AddMaintenance(int equipmentId, string note, string performedBy)
    {
        _context.MaintenanceRecords.Add(new MaintenanceRecord
        {
            EquipmentId = equipmentId,
            MaintenanceDate = DateTime.UtcNow,
            Description = string.IsNullOrWhiteSpace(note)
                ? "Kiểm tra tài sản sau khi trả."
                : note,
            Cost = 0,
            PerformedBy = performedBy
        });
    }
}
