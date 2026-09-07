using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LabManagementAPI.Data;
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
public class HandoverController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly IFileStorage _fileStorage;
    private readonly IConfiguration _configuration;
    private readonly IApprovalDelegationService _approvalDelegationService;

    public HandoverController(
        AppDbContext context,
        IAuditService auditService,
        INotificationService notificationService,
        IFileStorage fileStorage,
        IConfiguration configuration,
        IApprovalDelegationService? approvalDelegationService = null)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
        _fileStorage = fileStorage;
        _configuration = configuration;
        _approvalDelegationService = approvalDelegationService ?? new ApprovalDelegationService(context);
    }

    public sealed class HandoverItemDto
    {
        public int EquipmentId { get; set; }
        [Required, MaxLength(50)] public string Condition { get; set; } = EquipmentStatuses.Available;
        [MaxLength(1000)] public string Accessories { get; set; } = string.Empty;
        [MaxLength(2000)] public string Note { get; set; } = string.Empty;
    }

    public sealed class CreateHandoverDto
    {
        [Range(1, int.MaxValue)] public int BorrowRecordId { get; set; }
        [MaxLength(2000)] public string Notes { get; set; } = string.Empty;
        [MinLength(1)] public List<HandoverItemDto> Items { get; set; } = new();
    }

    public sealed class CreateIssueReportDto
    {
        [Range(1, int.MaxValue)] public int EquipmentId { get; set; }
        [Required, MaxLength(50)] public string IssueType { get; set; } = string.Empty;
        [Required, MaxLength(2000)] public string Description { get; set; } = string.Empty;
        public List<IFormFile> Files { get; set; } = new();
    }

    public sealed class ResolveIssueReportDto
    {
        [Required, MaxLength(50)] public string Action { get; set; } = string.Empty;
        [Required, MaxLength(2000)] public string Note { get; set; } = string.Empty;
    }

    [HttpGet("{borrowRecordId:int}")]
    public async Task<ActionResult<object>> Get(int borrowRecordId, CancellationToken cancellationToken)
    {
        var handover = await _context.HandoverRecords.AsNoTracking()
            .Include(item => item.BorrowRecord)
            .Include(item => item.Items).ThenInclude(item => item.Equipment)
            .Include(item => item.Evidence)
            .Include(item => item.IssueReports)
                .ThenInclude(issue => issue.Evidence)
            .SingleOrDefaultAsync(item => item.BorrowRecordId == borrowRecordId, cancellationToken);
        if (handover is null) return NotFound(new { message = "Phiếu chưa có biên bản bàn giao." });
        var userId = GetCurrentUserId();
        if (!await CanHandoverBorrowAsync(cancellationToken) && handover.BorrowRecord?.UserId != userId) return Forbid();
        return Ok(new
        {
            handover.Id, handover.Code, handover.BorrowRecordId, handover.HandoverAt, handover.Notes, handover.ConfirmedAt,
            canConfirm = handover.BorrowRecord?.UserId == userId
                && handover.BorrowRecord.Status == BorrowStatuses.Approved
                && handover.ConfirmedAt is null
                && !handover.IssueReports.Any(issue => issue.Status == HandoverIssueReportStatuses.Pending),
            canReportIssue = handover.BorrowRecord?.UserId == userId
                && handover.BorrowRecord.Status == BorrowStatuses.Approved
                && handover.ConfirmedAt is null,
            hasPendingIssueReports = handover.IssueReports.Any(issue => issue.Status == HandoverIssueReportStatuses.Pending),
            issueReports = handover.IssueReports.OrderByDescending(issue => issue.ReportedAt).Select(issue => new
            {
                issue.Id,
                issue.EquipmentId,
                issue.IssueType,
                issue.Description,
                issue.Status,
                issue.ResolutionAction,
                issue.ResolutionNote,
                issue.ReportedAt,
                issue.ResolvedAt,
                evidence = issue.Evidence.OrderByDescending(evidence => evidence.UploadedAt).Select(evidence => new
                {
                    evidence.Id,
                    evidence.OriginalFileName,
                    evidence.ContentType,
                    evidence.FileSize,
                    evidence.UploadedAt
                })
            }),
            items = handover.Items.Select(item => new
            {
                item.EquipmentId, equipmentName = item.Equipment!.Name, serial = item.Equipment.Serial,
                item.Condition, item.Accessories, item.Note
            }),
            evidence = handover.Evidence.OrderByDescending(item => item.UploadedAt).Select(item => new
            {
                item.Id, item.EquipmentId, item.EvidenceType, item.OriginalFileName,
                item.ContentType, item.FileSize, item.UploadedAt
            })
        });
    }

    [HttpPost("{borrowRecordId:int}/issue-reports")]
    [Authorize(Roles = Roles.Borrowers)]
    [EnableRateLimiting("sensitive")]
    [RequestSizeLimit(55_000_000)]
    public async Task<ActionResult<object>> CreateIssueReport(
        int borrowRecordId,
        [FromForm] CreateIssueReportDto dto,
        CancellationToken cancellationToken)
    {
        dto.IssueType = dto.IssueType.Trim().ToUpperInvariant();
        dto.Description = dto.Description.Trim();
        if (!HandoverIssueTypes.All.Contains(dto.IssueType))
            return BadRequest(new { message = "Loại sai lệch không hợp lệ." });
        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest(new { message = "Vui lòng mô tả sai lệch thực tế." });
        if (dto.Files.Count > 5)
            return BadRequest(new { message = "Mỗi báo cáo được đính kèm tối đa 5 ảnh." });

        var userId = GetCurrentUserId();
        var handover = await _context.HandoverRecords
            .Include(item => item.BorrowRecord)
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.BorrowRecordId == borrowRecordId, cancellationToken);
        if (handover?.BorrowRecord is null)
            return NotFound(new { message = "Không tìm thấy biên bản bàn giao." });
        if (handover.BorrowRecord.UserId != userId) return Forbid();
        if (handover.ConfirmedAt.HasValue || handover.BorrowRecord.Status != BorrowStatuses.Approved)
            return Conflict(new { message = "Chỉ được báo sai lệch trước khi xác nhận nhận tài sản." });
        if (!handover.Items.Any(item => item.EquipmentId == dto.EquipmentId))
            return BadRequest(new { message = "Tài sản không thuộc biên bản bàn giao." });
        if (await _context.HandoverIssueReports.AnyAsync(
                issue => issue.HandoverRecordId == handover.Id
                    && issue.EquipmentId == dto.EquipmentId
                    && issue.Status == HandoverIssueReportStatuses.Pending,
                cancellationToken))
            return Conflict(new { message = "Tài sản này đã có báo cáo sai lệch đang chờ xử lý." });

        var storedFiles = new List<StoredFile>();
        try
        {
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".jpg", ".jpeg", ".png", ".webp" };
            var maxFileBytes = _configuration.GetValue("Uploads:MaxEvidenceFileBytes", 10 * 1024 * 1024L);
            foreach (var file in dto.Files)
            {
                storedFiles.Add(await _fileStorage.SaveAsync(
                    file,
                    "handover-issues",
                    allowedExtensions,
                    maxFileBytes,
                    cancellationToken));
            }
        }
        catch (InvalidDataException exception)
        {
            foreach (var storedFile in storedFiles)
                await _fileStorage.DeleteAsync(storedFile.StoredPath, cancellationToken);
            return BadRequest(new { message = exception.Message });
        }

        var report = new HandoverIssueReport
        {
            HandoverRecordId = handover.Id,
            EquipmentId = dto.EquipmentId,
            ReportedByUserId = userId,
            IssueType = dto.IssueType,
            Description = dto.Description,
            Status = HandoverIssueReportStatuses.Pending,
            ReportedAt = DateTime.UtcNow,
            Evidence = storedFiles.Select(storedFile => new HandoverIssueEvidence
            {
                OriginalFileName = storedFile.OriginalFileName,
                StoredPath = storedFile.StoredPath,
                ContentType = storedFile.ContentType,
                FileSize = storedFile.Length,
                UploadedByUserId = userId
            }).ToList()
        };
        _context.HandoverIssueReports.Add(report);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var storedFile in storedFiles)
                await _fileStorage.DeleteAsync(storedFile.StoredPath, cancellationToken);
            throw;
        }
        await _auditService.WriteAsync(
            HttpContext,
            "CreateIssueReport",
            nameof(HandoverIssueReport),
            report.Id,
            new { report.HandoverRecordId, report.EquipmentId, report.IssueType },
            cancellationToken);
        await _notificationService.NotifyManagersAsync(
            "HANDOVER_ISSUE_REPORTED",
            "Có báo cáo sai lệch bàn giao",
            $"{handover.BorrowRecord.User?.FullName ?? "Người mượn"} đã báo sai lệch cho biên bản {handover.Code}. Vui lòng kiểm tra tại Lab.",
            "/dashboard/handover-issues",
            cancellationToken);

        return Ok(new { report.Id, evidenceCount = storedFiles.Count, message = "Đã gửi báo cáo sai lệch. Vui lòng chờ quản lý kiểm tra tại Lab." });
    }

    [HttpGet("issue-reports")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult<IEnumerable<object>>> GetIssueReports(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = _context.HandoverIssueReports
            .AsNoTracking()
            .Include(issue => issue.HandoverRecord)
                .ThenInclude(handover => handover!.BorrowRecord)
                    .ThenInclude(record => record!.User)
            .Include(issue => issue.Equipment)
            .Include(issue => issue.ReportedByUser)
            .Include(issue => issue.ResolvedByUser)
            .Include(issue => issue.Evidence)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(issue => issue.Status == status.Trim());

        var reports = await query
            .OrderByDescending(issue => issue.Status == HandoverIssueReportStatuses.Pending)
            .ThenByDescending(issue => issue.ReportedAt)
            .Select(issue => new
            {
                issue.Id,
                issue.HandoverRecordId,
                borrowRecordId = issue.HandoverRecord!.BorrowRecordId,
                handoverCode = issue.HandoverRecord.Code,
                borrowerName = issue.HandoverRecord.BorrowRecord!.User!.FullName,
                borrowerUsername = issue.HandoverRecord.BorrowRecord.User.Username,
                equipmentId = issue.EquipmentId,
                equipmentName = issue.Equipment!.Name,
                assetCode = issue.Equipment.AssetCode,
                serial = issue.Equipment.Serial,
                issue.IssueType,
                issue.Description,
                issue.Status,
                issue.ResolutionAction,
                issue.ResolutionNote,
                reportedAt = issue.ReportedAt,
                resolvedAt = issue.ResolvedAt,
                resolvedByName = issue.ResolvedByUser == null ? null : issue.ResolvedByUser.FullName,
                evidence = issue.Evidence.OrderByDescending(evidence => evidence.UploadedAt).Select(evidence => new
                {
                    evidence.Id,
                    evidence.OriginalFileName,
                    evidence.ContentType,
                    evidence.FileSize,
                    evidence.UploadedAt
                })
            })
            .ToListAsync(cancellationToken);
        return Ok(reports);
    }

    [HttpGet("issue-reports/{reportId:int}/evidence/{evidenceId:long}")]
    public async Task<IActionResult> DownloadIssueEvidence(
        int reportId,
        long evidenceId,
        CancellationToken cancellationToken)
    {
        var evidence = await _context.HandoverIssueEvidence.AsNoTracking()
            .Include(item => item.HandoverIssueReport)
                .ThenInclude(report => report!.HandoverRecord)
                    .ThenInclude(handover => handover!.BorrowRecord)
            .SingleOrDefaultAsync(item => item.Id == evidenceId
                && item.HandoverIssueReportId == reportId, cancellationToken);
        if (evidence is null) return NotFound();

        var borrowerId = evidence.HandoverIssueReport?.HandoverRecord?.BorrowRecord?.UserId;
        if (!await CanHandoverBorrowAsync(cancellationToken) && borrowerId != GetCurrentUserId())
            return Forbid();

        var stream = await _fileStorage.OpenReadAsync(evidence.StoredPath, cancellationToken);
        if (stream is null) return NotFound();
        return File(stream, evidence.ContentType, evidence.OriginalFileName, enableRangeProcessing: true);
    }

    [HttpPut("issue-reports/{id:int}/resolve")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> ResolveIssueReport(
        int id,
        [FromBody] ResolveIssueReportDto dto,
        CancellationToken cancellationToken)
    {
        dto.Action = dto.Action.Trim().ToUpperInvariant();
        dto.Note = dto.Note.Trim();
        if (!HandoverIssueReportActions.All.Contains(dto.Action))
            return BadRequest(new { message = "Hướng xử lý sai lệch không hợp lệ." });
        if (string.IsNullOrWhiteSpace(dto.Note))
            return BadRequest(new { message = "Vui lòng nhập ghi chú xử lý." });

        var report = await _context.HandoverIssueReports
            .Include(issue => issue.HandoverRecord)
                .ThenInclude(handover => handover!.BorrowRecord)
            .SingleOrDefaultAsync(issue => issue.Id == id, cancellationToken);
        if (report is null) return NotFound(new { message = "Không tìm thấy báo cáo sai lệch." });
        if (report.Status != HandoverIssueReportStatuses.Pending)
            return Conflict(new { message = "Báo cáo này đã được xử lý trước đó." });

        report.Status = dto.Action == HandoverIssueReportActions.Reject
            ? HandoverIssueReportStatuses.Rejected
            : HandoverIssueReportStatuses.Resolved;
        report.ResolutionAction = dto.Action;
        report.ResolutionNote = dto.Note;
        report.ResolvedByUserId = GetCurrentUserId();
        report.ResolvedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "ResolveIssueReport",
            nameof(HandoverIssueReport),
            report.Id,
            new { report.Status, report.ResolutionAction, report.ResolutionNote },
            cancellationToken);
        if (report.HandoverRecord?.BorrowRecord is not null)
        {
            var message = report.Status == HandoverIssueReportStatuses.Rejected
                ? "Báo cáo sai lệch của bạn đã được quản lý kiểm tra và từ chối."
                : "Báo cáo sai lệch của bạn đã được quản lý kiểm tra và ghi nhận. Vui lòng kiểm tra lại thiết bị tại Lab rồi xác nhận nhận tài sản.";
            await _notificationService.NotifyUserAsync(
                report.HandoverRecord.BorrowRecord.UserId,
                "HANDOVER_ISSUE_RESOLVED",
                "Đã xử lý báo cáo sai lệch",
                message,
                "/dashboard/borrow-history",
                cancellationToken);
        }
        return Ok(new { message = "Đã cập nhật kết quả xử lý báo cáo sai lệch." });
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateHandoverDto dto, CancellationToken cancellationToken)
    {
        if (!await CanHandoverBorrowAsync(cancellationToken)) return Forbid();

        dto.Notes = dto.Notes.Trim();
        foreach (var item in dto.Items)
        {
            item.Condition = item.Condition.Trim(); item.Accessories = item.Accessories.Trim(); item.Note = item.Note.Trim();
            if (item.Condition is not (EquipmentStatuses.Available
                or "SCRATCHED"
                or "MISSING_ACCESSORIES"
                or EquipmentStatuses.Broken))
                return BadRequest(new { message = "Tình trạng bàn giao không hợp lệ." });
        }

        var record = await _context.BorrowRecords
            .Include(item => item.Details)
                .ThenInclude(item => item.Equipment)
            .SingleOrDefaultAsync(item => item.Id == dto.BorrowRecordId, cancellationToken);
        if (record is null) return NotFound(new { message = "Không tìm thấy phiếu mượn." });
        if (record.Status != BorrowStatuses.Approved) return Conflict(new { message = "Chỉ được lập biên bản cho phiếu đã duyệt và đang chờ bàn giao." });
        if (record.HoldExpiresAt.HasValue && record.HoldExpiresAt.Value <= DateTime.UtcNow)
            return Conflict(new { message = "Thời gian giữ chỗ đã hết hạn. Vui lòng tạo lại yêu cầu mượn." });
        if (await _context.HandoverRecords.AnyAsync(item => item.BorrowRecordId == record.Id, cancellationToken))
            return Conflict(new { message = "Phiếu đã có biên bản bàn giao." });

        if (record.Details.Any(item => item.Equipment?.Status != EquipmentStatuses.BorrowPending))
            return Conflict(new { message = "Một hoặc nhiều tài sản không còn ở trạng thái giữ chỗ để bàn giao." });

        var detailIds = record.Details.Select(item => item.EquipmentId).Distinct().ToHashSet();
        var submittedIds = dto.Items.Select(item => item.EquipmentId).Distinct().ToHashSet();
        if (!detailIds.SetEquals(submittedIds)) return BadRequest(new { message = "Biên bản phải ghi nhận đủ từng tài sản trong phiếu." });

        var handover = new HandoverRecord
        {
            Code = $"BH-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..28],
            BorrowRecordId = record.Id, HandedOverByUserId = GetCurrentUserId(), ReceivedByUserId = record.UserId,
            Notes = dto.Notes, ConfirmedAt = null,
            Items = dto.Items.Select(item => new HandoverItem
            {
                EquipmentId = item.EquipmentId, Condition = item.Condition, Accessories = item.Accessories, Note = item.Note
            }).ToList()
        };
        _context.HandoverRecords.Add(handover);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "Create", nameof(HandoverRecord), handover.Id, new { handover.Code, handover.BorrowRecordId }, cancellationToken);
        await _notificationService.NotifyUserAsync(record.UserId, "HANDOVER_CREATED", "Cần xác nhận nhận tài sản", $"Biên bản {handover.Code} đã được lập. Vui lòng kiểm tra và xác nhận đã nhận tài sản.", "/dashboard/borrow-history", cancellationToken);
        return Ok(new { handover.Id, handover.Code, message = "Đã lập biên bản. Đang chờ người nhận xác nhận." });
    }

    [HttpPost("{borrowRecordId:int}/confirm-receipt")]
    [Authorize(Roles = Roles.Borrowers)]
    public async Task<IActionResult> ConfirmReceipt(
        int borrowRecordId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var handover = await _context.HandoverRecords
            .AsNoTracking()
            .Include(item => item.BorrowRecord)
                .ThenInclude(item => item!.Details)
            .SingleOrDefaultAsync(item => item.BorrowRecordId == borrowRecordId, cancellationToken);
        if (handover?.BorrowRecord is null)
            return NotFound(new { message = "Không tìm thấy biên bản bàn giao." });
        if (handover.BorrowRecord.UserId != userId) return Forbid();
        if (handover.ConfirmedAt.HasValue)
            return Conflict(new { message = "Biên bản đã được xác nhận trước đó." });
        if (await _context.HandoverIssueReports.AnyAsync(
                issue => issue.HandoverRecordId == handover.Id
                    && issue.Status == HandoverIssueReportStatuses.Pending,
                cancellationToken))
            return Conflict(new { message = "Có báo cáo sai lệch đang chờ quản lý xử lý; chưa thể xác nhận nhận tài sản." });
        if (handover.BorrowRecord.Status is not (BorrowStatuses.Approved or BorrowStatuses.Borrowed))
            return Conflict(new { message = "Phiếu không ở trạng thái chờ xác nhận nhận tài sản." });

        var isLegacyBorrowedRecord = handover.BorrowRecord.Status == BorrowStatuses.Borrowed;
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var confirmed = await _context.HandoverRecords
            .Where(item => item.Id == handover.Id && item.ConfirmedAt == null)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.ConfirmedAt, DateTime.UtcNow)
                    .SetProperty(item => item.ReceivedByUserId, userId),
                cancellationToken);
        if (confirmed == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Biên bản đã được xác nhận bởi một phiên khác." });
        }

        var equipmentIds = handover.BorrowRecord.Details
            .Select(item => item.EquipmentId)
            .Distinct()
            .ToArray();
        if (!isLegacyBorrowedRecord)
        {
            var movedEquipment = await _context.Equipments
                .Where(item => equipmentIds.Contains(item.Id)
                    && item.Status == EquipmentStatuses.BorrowPending)
                .ExecuteUpdateAsync(
                    updates => updates
                        .SetProperty(item => item.Status, EquipmentStatuses.Borrowed)
                        .SetProperty(item => item.BorrowCount, item => item.BorrowCount + 1),
                    cancellationToken);
            if (movedEquipment != equipmentIds.Length)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict(new { message = "Trạng thái tài sản đã thay đổi; chưa thể xác nhận nhận." });
            }

            await _context.BorrowRequestDetails
                .Where(item => item.BorrowRecordId == borrowRecordId && item.Status == BorrowStatuses.Approved)
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(item => item.Status, BorrowStatuses.Borrowed),
                    cancellationToken);
            var movedRecord = await _context.BorrowRecords
                .Where(item => item.Id == borrowRecordId && item.Status == BorrowStatuses.Approved)
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(item => item.Status, BorrowStatuses.Borrowed),
                    cancellationToken);
            if (movedRecord == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict(new { message = "Phiếu đã được xử lý bởi một phiên khác." });
            }

            _context.BorrowStatusHistories.Add(new BorrowStatusHistory
            {
                BorrowRecordId = borrowRecordId,
                FromStatus = BorrowStatuses.Approved,
                ToStatus = BorrowStatuses.Borrowed,
                Note = "Người mượn xác nhận đã nhận đủ tài sản theo biên bản bàn giao.",
                ChangedByUserId = userId
            });
        }

        await _auditService.WriteAsync(
            HttpContext,
            "ConfirmReceipt",
            nameof(HandoverRecord),
            handover.Id,
            new { handover.Code, BorrowRecordId = borrowRecordId, EquipmentIds = equipmentIds },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _notificationService.NotifyManagersAsync(
            "HANDOVER_CONFIRMED",
            "Người mượn đã xác nhận nhận tài sản",
            $"Biên bản {handover.Code} đã được người nhận xác nhận.",
            "/dashboard/borrow-requests",
            cancellationToken);
        return Ok(new { message = "Đã xác nhận nhận tài sản. Phiếu chuyển sang trạng thái đang mượn." });
    }

    public sealed class UploadEvidenceDto
    {
        [Required] public IFormFile? File { get; set; }
        public int? EquipmentId { get; set; }
        [Required, MaxLength(50)] public string EvidenceType { get; set; } = "PHOTO";
    }

    [HttpPost("{borrowRecordId:int}/evidence")]
    [EnableRateLimiting("sensitive")]
    [RequestSizeLimit(11_000_000)]
    public async Task<IActionResult> UploadEvidence(
        int borrowRecordId,
        [FromForm] UploadEvidenceDto dto,
        CancellationToken cancellationToken)
    {
        if (!await CanHandoverBorrowAsync(cancellationToken)) return Forbid();

        if (dto.File is null) return BadRequest(new { message = "Vui lòng chọn file minh chứng." });
        dto.EvidenceType = dto.EvidenceType.Trim().ToUpperInvariant();
        if (dto.EvidenceType is not ("PHOTO" or "DOCUMENT" or "SIGNATURE"))
            return BadRequest(new { message = "Loại minh chứng không hợp lệ." });

        var handover = await _context.HandoverRecords
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.BorrowRecordId == borrowRecordId, cancellationToken);
        if (handover is null) return NotFound(new { message = "Không tìm thấy biên bản bàn giao." });
        if (dto.EquipmentId.HasValue && !handover.Items.Any(item => item.EquipmentId == dto.EquipmentId.Value))
            return BadRequest(new { message = "Tài sản không thuộc biên bản bàn giao." });

        StoredFile stored;
        try
        {
            stored = await _fileStorage.SaveAsync(
                dto.File,
                "handovers",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".doc", ".docx" },
                _configuration.GetValue("Uploads:MaxEvidenceFileBytes", 10 * 1024 * 1024L),
                cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        var evidence = new HandoverEvidence
        {
            HandoverRecordId = handover.Id,
            EquipmentId = dto.EquipmentId,
            EvidenceType = dto.EvidenceType,
            OriginalFileName = stored.OriginalFileName,
            StoredPath = stored.StoredPath,
            ContentType = stored.ContentType,
            FileSize = stored.Length,
            UploadedByUserId = GetCurrentUserId()
        };
        _context.HandoverEvidence.Add(evidence);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "UploadEvidence", nameof(HandoverRecord), handover.Id,
            new { evidence.Id, evidence.EvidenceType, evidence.EquipmentId }, cancellationToken);
        return Ok(new { evidence.Id, evidence.OriginalFileName, evidence.EvidenceType, message = "Đã lưu file minh chứng." });
    }

    [HttpGet("{borrowRecordId:int}/evidence/{evidenceId:long}")]
    public async Task<IActionResult> DownloadEvidence(
        int borrowRecordId,
        long evidenceId,
        CancellationToken cancellationToken)
    {
        var evidence = await _context.HandoverEvidence.AsNoTracking()
            .Include(item => item.HandoverRecord)
                .ThenInclude(item => item!.BorrowRecord)
            .SingleOrDefaultAsync(item => item.Id == evidenceId && item.HandoverRecord!.BorrowRecordId == borrowRecordId, cancellationToken);
        if (evidence is null) return NotFound();
        if (!await CanHandoverBorrowAsync(cancellationToken)
            && evidence.HandoverRecord?.BorrowRecord?.UserId != GetCurrentUserId()) return Forbid();
        var stream = await _fileStorage.OpenReadAsync(evidence.StoredPath, cancellationToken);
        if (stream is null) return NotFound();
        return File(stream, evidence.ContentType, evidence.OriginalFileName, enableRangeProcessing: true);
    }

    [HttpDelete("{borrowRecordId:int}/evidence/{evidenceId:long}")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> DeleteEvidence(
        int borrowRecordId,
        long evidenceId,
        CancellationToken cancellationToken)
    {
        var evidence = await _context.HandoverEvidence
            .SingleOrDefaultAsync(item => item.Id == evidenceId && item.HandoverRecord!.BorrowRecordId == borrowRecordId, cancellationToken);
        if (evidence is null) return NotFound();
        var path = evidence.StoredPath;
        _context.HandoverEvidence.Remove(evidence);
        await _context.SaveChangesAsync(cancellationToken);
        await _fileStorage.DeleteAsync(path, cancellationToken);
        return NoContent();
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Task<bool> CanHandoverBorrowAsync(CancellationToken cancellationToken)
    {
        return _approvalDelegationService.CanHandoverAsync(
            GetCurrentUserId(),
            User.FindFirstValue(ClaimTypes.Role),
            ApprovalDelegationScopes.BorrowRequest,
            cancellationToken);
    }

    private bool IsManager() => User.IsInRole(Roles.Admin)
        || User.IsInRole(Roles.LabHead)
        || User.IsInRole(Roles.DeputyLabHead);
}
