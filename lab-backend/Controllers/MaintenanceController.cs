using System.ComponentModel.DataAnnotations;
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
[Authorize(Roles = Roles.Managers)]
public class MaintenanceController : ControllerBase
{
    private const string InProgress = MaintenanceStatuses.InProgress;
    private const string Completing = MaintenanceStatuses.Completing;
    private const string Completed = MaintenanceStatuses.Completed;

    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly IFileStorage _fileStorage;
    private readonly IConfiguration _configuration;

    public MaintenanceController(
        AppDbContext context,
        IAuditService auditService,
        INotificationService notificationService,
        IFileStorage fileStorage,
        IConfiguration configuration)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
        _fileStorage = fileStorage;
        _configuration = configuration;
    }

    public sealed class CreateMaintenanceDto
    {
        [Range(1, int.MaxValue)]
        public int EquipmentId { get; set; }

        public DateTime MaintenanceDate { get; set; }

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Cost { get; set; }

        [Required, MaxLength(255)]
        public string PerformedBy { get; set; } = string.Empty;

        [MaxLength(255)] public string Supplier { get; set; } = string.Empty;
        [MaxLength(4000)] public string Checklist { get; set; } = string.Empty;
    }

    public sealed class CompleteMaintenanceDto
    {
        [Required, MaxLength(2000)]
        public string Result { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string NextEquipmentStatus { get; set; } = EquipmentStatuses.Available;

        [MaxLength(4000)] public string ChecklistResult { get; set; } = string.Empty;
        public List<MaintenancePartDto> Parts { get; set; } = [];
    }

    public sealed class MaintenancePartDto
    {
        [Range(1, int.MaxValue)] public int ConsumableId { get; set; }
        [Range(1, int.MaxValue)] public int Quantity { get; set; }
        [Range(0, double.MaxValue)] public decimal? UnitCost { get; set; }
        [MaxLength(1000)] public string Note { get; set; } = string.Empty;
    }

    public sealed class UploadEvidenceDto
    {
        [Required] public IFormFile? File { get; set; }
        [Required, MaxLength(50)] public string EvidenceType { get; set; } = "PHOTO";
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetMaintenanceRecords(
        CancellationToken cancellationToken)
    {
        var records = await _context.MaintenanceRecords
            .AsNoTracking()
            .Include(record => record.Equipment)
            .Include(record => record.Parts)
                .ThenInclude(part => part.Consumable)
            .Include(record => record.Evidence)
            .OrderByDescending(record => record.MaintenanceDate)
            .Select(record => new
            {
                id = record.Id,
                equipmentId = record.EquipmentId,
                device = record.Equipment!.Name,
                maintenanceDate = record.MaintenanceDate,
                description = record.Description,
                cost = record.Cost,
                performedBy = record.PerformedBy,
                status = record.Status,
                completedAt = record.CompletedAt,
                result = record.Result,
                resultStatus = record.ResultStatus,
                record.Supplier,
                record.Checklist,
                record.ChecklistResult,
                parts = record.Parts.Select(part => new
                {
                    part.ConsumableId,
                    consumableName = part.Consumable!.Name,
                    part.Quantity,
                    part.UnitCost,
                    part.Note
                }),
                evidence = record.Evidence.Select(evidence => new
                {
                    evidence.Id,
                    evidence.EvidenceType,
                    evidence.OriginalFileName,
                    evidence.ContentType,
                    evidence.FileSize,
                    evidence.UploadedAt
                })
            })
            .ToListAsync(cancellationToken);

        return Ok(records);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetMaintenanceRecordsPaged(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        var query = _context.MaintenanceRecords
            .AsNoTracking()
            .Include(record => record.Equipment)
            .Include(record => record.Parts)
                .ThenInclude(part => part.Consumable)
            .Include(record => record.Evidence)
            .AsQueryable();
        var search = paging.NormalizedSearch;
        if (search.Length > 0)
        {
            query = query.Where(record =>
                record.Equipment!.Name.Contains(search)
                || record.Equipment.Serial.Contains(search)
                || record.Description.Contains(search)
                || record.PerformedBy.Contains(search)
                || record.Supplier.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            var status = paging.Status.Trim();
            query = query.Where(record => record.Status == status);
        }
        if (paging.From.HasValue)
        {
            query = query.Where(record => record.MaintenanceDate >= paging.From.Value);
        }
        if (paging.To.HasValue)
        {
            var exclusiveTo = paging.To.Value.Date.AddDays(1);
            query = query.Where(record => record.MaintenanceDate < exclusiveTo);
        }

        var page = await query
            .OrderByDescending(record => record.MaintenanceDate)
            .ThenByDescending(record => record.Id)
            .ToPagedResultAsync(paging, cancellationToken);
        var items = page.Items.Select(record => (object)new
        {
            id = record.Id,
            equipmentId = record.EquipmentId,
            device = record.Equipment!.Name,
            maintenanceDate = record.MaintenanceDate,
            description = record.Description,
            cost = record.Cost,
            performedBy = record.PerformedBy,
            status = record.Status,
            completedAt = record.CompletedAt,
            result = record.Result,
            resultStatus = record.ResultStatus,
            record.Supplier,
            record.Checklist,
            record.ChecklistResult,
            parts = record.Parts.Select(part => new
            {
                part.ConsumableId,
                consumableName = part.Consumable!.Name,
                part.Quantity,
                part.UnitCost,
                part.Note
            }),
            evidence = record.Evidence.Select(evidence => new
            {
                evidence.Id,
                evidence.EvidenceType,
                evidence.OriginalFileName,
                evidence.ContentType,
                evidence.FileSize,
                evidence.UploadedAt
            })
        }).ToList();
        return Ok(new PagedResult<object>(items, page.Total, page.Page, page.PageSize, page.TotalPages));
    }

    [HttpPost]
    public async Task<ActionResult<MaintenanceRecord>> CreateMaintenance(
        [FromBody] CreateMaintenanceDto dto,
        CancellationToken cancellationToken)
    {
        dto.Description = dto.Description.Trim();
        dto.PerformedBy = dto.PerformedBy.Trim();
        dto.Supplier = dto.Supplier.Trim();
        dto.Checklist = dto.Checklist.Trim();
        if (string.IsNullOrWhiteSpace(dto.Description)
            || string.IsNullOrWhiteSpace(dto.PerformedBy))
        {
            return BadRequest(new { message = "Nội dung và người thực hiện là bắt buộc." });
        }

        if (await HasLockedBorrowRequestAsync(dto.EquipmentId, cancellationToken)
            || await _context.Equipments.AnyAsync(
                equipment => equipment.Id == dto.EquipmentId
                    && equipment.Status == EquipmentStatuses.BorrowPending,
                cancellationToken))
        {
            return Conflict(new
            {
                message = "Không thể tạo bảo trì khi thiết bị đang có phiếu mượn chờ xử lý/bàn giao."
            });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var claimedEquipment = await _context.Equipments
            .Where(equipment => equipment.Id == dto.EquipmentId
                && equipment.Status != EquipmentStatuses.Borrowed
                && equipment.Status != EquipmentStatuses.BorrowPending)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(
                    equipment => equipment.Status,
                    EquipmentStatuses.MaintenanceInProgress),
                cancellationToken);
        if (claimedEquipment == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            var exists = await _context.Equipments.AnyAsync(
                equipment => equipment.Id == dto.EquipmentId,
                cancellationToken);
            if (exists)
            {
                return Conflict(new { message = "Không thể tạo bảo trì khi thiết bị đang được mượn." });
            }

            return BadRequest(new { message = "Thiết bị không tồn tại." });
        }

        var hasActiveMaintenance = await _context.MaintenanceRecords.AnyAsync(
            record => record.EquipmentId == dto.EquipmentId
                && record.Status == InProgress,
            cancellationToken);
        if (hasActiveMaintenance)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Thiết bị đã có một phiếu bảo trì đang xử lý." });
        }

        var record = new MaintenanceRecord
        {
            EquipmentId = dto.EquipmentId,
            MaintenanceDate = dto.MaintenanceDate == default
                ? DateTime.UtcNow
                : dto.MaintenanceDate,
            Description = dto.Description,
            Cost = dto.Cost,
            PerformedBy = dto.PerformedBy,
            Supplier = dto.Supplier,
            Checklist = dto.Checklist,
            Status = InProgress,
            ActiveEquipmentKey = $"EQ:{dto.EquipmentId}"
        };

        _context.MaintenanceRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
        await _notificationService.NotifyManagersAsync(
            "MAINTENANCE_CREATED",
            "Có phiếu bảo trì mới",
            $"Phiếu bảo trì #{record.Id} đã được tạo cho thiết bị.",
            "/dashboard/maintenance",
            cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Create",
            nameof(MaintenanceRecord),
            record.Id,
            new { record.EquipmentId, record.Cost },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(record);
    }

    [HttpPut("{id:int}/complete")]
    public async Task<IActionResult> CompleteMaintenance(
        int id,
        [FromBody] CompleteMaintenanceDto dto,
        CancellationToken cancellationToken)
    {
        dto.Result = dto.Result.Trim();
        dto.NextEquipmentStatus = EquipmentStatuses.Normalize(dto.NextEquipmentStatus);
        dto.ChecklistResult = dto.ChecklistResult.Trim();
        if (string.IsNullOrWhiteSpace(dto.Result))
        {
            return BadRequest(new { message = "Kết quả bảo trì là bắt buộc." });
        }
        if (dto.NextEquipmentStatus is not (EquipmentStatuses.Available
            or EquipmentStatuses.Broken
            or EquipmentStatuses.UnderWarranty
            or EquipmentStatuses.MaintenanceInProgress))
        {
            return BadRequest(new { message = "Trạng thái sau bảo trì không hợp lệ." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var claimed = await _context.MaintenanceRecords
            .Where(record => record.Id == id && record.Status == InProgress)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(record => record.Status, Completing),
                cancellationToken);
        if (claimed == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Phiếu bảo trì không tồn tại hoặc đã được xử lý." });
        }

        var record = await _context.MaintenanceRecords
            .Include(item => item.Equipment)
            .Include(item => item.Parts)
            .FirstAsync(item => item.Id == id, cancellationToken);

        var requestedParts = dto.Parts ?? [];
        if (requestedParts.GroupBy(part => part.ConsumableId).Any(group => group.Count() > 1))
        {
            await transaction.RollbackAsync(cancellationToken);
            return BadRequest(new { message = "Mỗi vật tư chỉ được khai báo một lần trong phiếu bảo trì." });
        }
        var partIds = requestedParts.Select(part => part.ConsumableId).ToHashSet();
        var consumables = await _context.Consumables
            .Where(consumable => partIds.Contains(consumable.Id))
            .ToDictionaryAsync(consumable => consumable.Id, cancellationToken);
        if (consumables.Count != partIds.Count)
        {
            await transaction.RollbackAsync(cancellationToken);
            return BadRequest(new { message = "Có vật tư không tồn tại." });
        }
        var today = VietnamTime.Today();
        var validLots = await _context.ConsumableLots
            .Where(lot => partIds.Contains(lot.ConsumableId)
                && lot.Quantity > 0
                && (!lot.ExpiryDate.HasValue || lot.ExpiryDate.Value >= VietnamTime.StartOfDayUtc(today)))
            .OrderBy(lot => lot.ExpiryDate == null)
            .ThenBy(lot => lot.ExpiryDate)
            .ThenBy(lot => lot.EntryDate)
            .ToListAsync(cancellationToken);
        foreach (var part in requestedParts)
        {
            var stock = consumables[part.ConsumableId];
            var beforeQuantity = stock.Quantity;
            if (beforeQuantity - stock.ReservedQuantity < part.Quantity)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict(new { message = $"Vật tư {stock.Name} không đủ tồn kho khả dụng; một phần đang được giữ cho phiếu cấp phát đã duyệt." });
            }

            var remaining = part.Quantity;
            var usedLots = new List<string>();
            foreach (var lot in validLots.Where(lot => lot.ConsumableId == stock.Id))
            {
                if (remaining == 0) break;
                var usedQuantity = Math.Min(remaining, lot.Quantity);
                if (usedQuantity == 0) continue;
                lot.Quantity -= usedQuantity;
                remaining -= usedQuantity;
                usedLots.Add($"{lot.LotNumber}:{usedQuantity}");
            }
            if (remaining > 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict(new { message = $"Các lô còn hạn của vật tư {stock.Name} không đủ số lượng để sử dụng." });
            }

            stock.Quantity -= part.Quantity;
            _context.ConsumableTransactions.Add(new ConsumableTransaction
            {
                ConsumableId = stock.Id,
                Type = "MAINTENANCE_USAGE",
                Quantity = part.Quantity,
                BeforeQuantity = beforeQuantity,
                AfterQuantity = stock.Quantity,
                Reason = $"Sử dụng cho phiếu bảo trì #{id}; lô {string.Join(", ", usedLots)}",
                UserId = GetCurrentUserId(),
                MaintenanceRecordId = id,
                CreatedAt = DateTime.UtcNow
            });
            record.Parts.Add(new MaintenancePartUsage
            {
                ConsumableId = stock.Id,
                Quantity = part.Quantity,
                UnitCost = part.UnitCost,
                Note = part.Note.Trim()
            });
        }
        record.Status = Completed;
        record.Result = dto.Result;
        record.ResultStatus = dto.NextEquipmentStatus;
        record.ChecklistResult = dto.ChecklistResult;
        record.ActiveEquipmentKey = null;
        record.CompletedAt = DateTime.UtcNow;
        if (record.Equipment is not null)
        {
            record.Equipment.Status = dto.NextEquipmentStatus;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _notificationService.NotifyManagersAsync(
            "MAINTENANCE_COMPLETED",
            "Phiếu bảo trì đã hoàn tất",
            $"Phiếu bảo trì #{id} đã hoàn tất với trạng thái thiết bị mới.",
            "/dashboard/maintenance",
            cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Complete",
            nameof(MaintenanceRecord),
            id,
            new { record.EquipmentId },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "Đã hoàn tất phiếu bảo trì và cập nhật trạng thái theo kết quả." });
    }

    [HttpPost("{id:int}/evidence")]
    [EnableRateLimiting("sensitive")]
    [RequestSizeLimit(11_000_000)]
    public async Task<IActionResult> UploadEvidence(
        int id,
        [FromForm] UploadEvidenceDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.File is null) return BadRequest(new { message = "Vui lòng chọn file minh chứng." });
        dto.EvidenceType = dto.EvidenceType.Trim().ToUpperInvariant();
        if (dto.EvidenceType is not ("PHOTO" or "DOCUMENT"))
            return BadRequest(new { message = "Loại minh chứng bảo trì không hợp lệ." });
        if (!await _context.MaintenanceRecords.AnyAsync(record => record.Id == id, cancellationToken))
            return NotFound(new { message = "Không tìm thấy phiếu bảo trì." });

        StoredFile stored;
        try
        {
            stored = await _fileStorage.SaveAsync(
                dto.File,
                "maintenance",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".doc", ".docx" },
                _configuration.GetValue("Uploads:MaxEvidenceFileBytes", 10 * 1024 * 1024L),
                cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        var evidence = new MaintenanceEvidence
        {
            MaintenanceRecordId = id,
            EvidenceType = dto.EvidenceType,
            OriginalFileName = stored.OriginalFileName,
            StoredPath = stored.StoredPath,
            ContentType = stored.ContentType,
            FileSize = stored.Length,
            UploadedByUserId = GetCurrentUserId()
        };
        _context.MaintenanceEvidence.Add(evidence);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "UploadEvidence", nameof(MaintenanceRecord), id,
            new { evidence.Id, evidence.EvidenceType }, cancellationToken);
        return Ok(new { evidence.Id, evidence.OriginalFileName, message = "Đã lưu file bảo trì." });
    }

    [HttpGet("{id:int}/evidence/{evidenceId:long}")]
    public async Task<IActionResult> DownloadEvidence(int id, long evidenceId, CancellationToken cancellationToken)
    {
        var evidence = await _context.MaintenanceEvidence.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == evidenceId && item.MaintenanceRecordId == id, cancellationToken);
        if (evidence is null) return NotFound();
        var stream = await _fileStorage.OpenReadAsync(evidence.StoredPath, cancellationToken);
        if (stream is null) return NotFound();
        return File(stream, evidence.ContentType, evidence.OriginalFileName, enableRangeProcessing: true);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteMaintenance(
        int id,
        CancellationToken cancellationToken)
    {
        var record = await _context.MaintenanceRecords.FindAsync(
            new object[] { id },
            cancellationToken);
        if (record is null)
        {
            return NotFound();
        }

        if (record.Status == InProgress)
        {
            return BadRequest(new { message = "Hãy hoàn tất phiếu bảo trì trước khi xóa." });
        }

        _context.MaintenanceRecords.Remove(record);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Delete",
            nameof(MaintenanceRecord),
            id,
            new { record.EquipmentId },
            cancellationToken);
        return NoContent();
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Task<bool> HasLockedBorrowRequestAsync(
        int equipmentId,
        CancellationToken cancellationToken)
        => _context.BorrowRecords
            .AsNoTracking()
            .AnyAsync(record =>
                (record.EquipmentId == equipmentId
                    || record.Details.Any(detail => detail.EquipmentId == equipmentId))
                && BorrowLockRules.EquipmentLockedBorrowStatuses.Contains(record.Status),
                cancellationToken);
}
