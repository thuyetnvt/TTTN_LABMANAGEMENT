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
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabManagementAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = Roles.Managers)]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly IFileStorage _fileStorage;
    private readonly IConfiguration _configuration;

    public InventoryController(
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

    public sealed class CreateInventoryDto
    {
        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        public int? LocationNodeId { get; set; }
        public int? AssetCategoryId { get; set; }
    }

    public sealed class ScanInventoryDto
    {
        [Required, MaxLength(128)]
        public string QrToken { get; set; } = string.Empty;
        public int? LocationNodeId { get; set; }
        public string Status { get; set; } = InventoryItemStatuses.Found;
        [MaxLength(2000)]
        public string Note { get; set; } = string.Empty;
    }

    public sealed class ReviewInventoryItemDto
    {
        [Required, MaxLength(50)]
        public string Resolution { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Note { get; set; } = string.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetSessions(CancellationToken cancellationToken)
    {
        var sessions = await _context.InventorySessions
            .AsNoTracking()
            .Include(session => session.Items)
            .OrderByDescending(session => session.StartedAt)
            .ToListAsync(cancellationToken);

        return Ok(sessions.Select(session => new
        {
            session.Id,
            session.Code,
            session.Name,
            session.Status,
            session.StartedAt,
            session.CompletedAt,
            session.LocationNodeId,
            total = session.Items.Count,
            found = session.Items.Count(item => item.Status == InventoryItemStatuses.Found),
            wrongLocation = session.Items.Count(item => item.Status == InventoryItemStatuses.WrongLocation),
            damaged = session.Items.Count(item => item.Status == InventoryItemStatuses.Damaged),
            missing = session.Items.Count(item => item.Status == InventoryItemStatuses.Missing),
            pending = session.Items.Count(item => item.Status == InventoryItemStatuses.Pending),
            unreviewed = session.Items.Count(item => item.Status != InventoryItemStatuses.Found && item.ReviewedAt == null)
        }));
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetSessionsPaged(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        var query = _context.InventorySessions
            .AsNoTracking()
            .Include(session => session.Items)
            .AsQueryable();
        var search = paging.NormalizedSearch;
        if (search.Length > 0)
        {
            query = query.Where(session => session.Code.Contains(search) || session.Name.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            var status = paging.Status.Trim();
            query = query.Where(session => session.Status == status);
        }
        if (paging.LocationNodeId.HasValue)
        {
            query = query.Where(session => session.LocationNodeId == paging.LocationNodeId.Value);
        }
        if (paging.CategoryId.HasValue)
        {
            query = query.Where(session => session.AssetCategoryId == paging.CategoryId.Value);
        }
        if (paging.From.HasValue)
        {
            query = query.Where(session => session.StartedAt >= paging.From.Value);
        }
        if (paging.To.HasValue)
        {
            var exclusiveTo = paging.To.Value.Date.AddDays(1);
            query = query.Where(session => session.StartedAt < exclusiveTo);
        }

        var page = await query
            .OrderByDescending(session => session.StartedAt)
            .ThenByDescending(session => session.Id)
            .ToPagedResultAsync(paging, cancellationToken);
        var items = page.Items.Select(session => (object)new
        {
            session.Id,
            session.Code,
            session.Name,
            session.Status,
            session.StartedAt,
            session.CompletedAt,
            session.LocationNodeId,
            total = session.Items.Count,
            found = session.Items.Count(item => item.Status == InventoryItemStatuses.Found),
            wrongLocation = session.Items.Count(item => item.Status == InventoryItemStatuses.WrongLocation),
            damaged = session.Items.Count(item => item.Status == InventoryItemStatuses.Damaged),
            missing = session.Items.Count(item => item.Status == InventoryItemStatuses.Missing),
            pending = session.Items.Count(item => item.Status == InventoryItemStatuses.Pending),
            unreviewed = session.Items.Count(item => item.Status != InventoryItemStatuses.Found && item.ReviewedAt == null)
        }).ToList();
        return Ok(new PagedResult<object>(items, page.Total, page.Page, page.PageSize, page.TotalPages));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetSession(int id, CancellationToken cancellationToken)
    {
        var session = await _context.InventorySessions
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.Code,
                item.Name,
                item.Status,
                item.StartedAt,
                item.CompletedAt,
                item.LocationNodeId,
                item.AssetCategoryId,
                locationName = item.LocationNode != null ? item.LocationNode.Name : null,
                categoryName = item.AssetCategory != null ? item.AssetCategory.Name : null,
                discrepancyCount = item.Items.Count(inventoryItem => inventoryItem.Status != InventoryItemStatuses.Found),
                unreviewedCount = item.Items.Count(inventoryItem => inventoryItem.Status != InventoryItemStatuses.Found && inventoryItem.ReviewedAt == null),
                canComplete = item.Status == InventoryStatuses.Reviewing
                    && item.Items.All(inventoryItem => inventoryItem.Status == InventoryItemStatuses.Found || inventoryItem.ReviewedAt != null),
                totalItems = item.Items.Count
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        }

        return Ok(session);
    }

    [HttpGet("{id:int}/items/paged")]
    public async Task<IActionResult> GetSessionItemsPaged(
        int id,
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        if (!await _context.InventorySessions.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken))
        {
            return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        }

        var query = _context.InventoryItems
            .AsNoTracking()
            .Where(item => item.InventorySessionId == id)
            .Include(item => item.Equipment)
            .Include(item => item.ActualLocationNode)
            .Include(item => item.InventoryItemEvidence)
            .AsQueryable();
        var search = paging.NormalizedSearch;
        if (search.Length > 0)
        {
            query = query.Where(item =>
                item.Equipment!.Name.Contains(search)
                || item.Equipment.AssetCode.Contains(search)
                || item.Equipment.Serial.Contains(search)
                || item.ExpectedLocationName.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            var status = paging.Status.Trim();
            query = query.Where(item => item.Status == status);
        }

        var page = await query
            .OrderBy(item => item.Equipment!.Name)
            .ThenBy(item => item.Id)
            .ToPagedResultAsync(paging, cancellationToken);
        var items = page.Items.Select(item => (object)new
        {
            item.Id,
            item.EquipmentId,
            assetCode = item.Equipment!.AssetCode,
            qrToken = item.Equipment.QrToken,
            equipmentName = item.Equipment.Name,
            serial = item.Equipment.Serial,
            expectedLocation = item.ExpectedLocationName,
            item.ActualLocationNodeId,
            actualLocation = item.ActualLocationNode?.Name,
            item.Status,
            bookQuantity = 1,
            actualQuantity = InventoryActualQuantity(item.Status),
            quantityDifference = InventoryQuantityDifference(item.Status),
            item.ScannedAt,
            item.Note,
            item.ReviewResolution,
            item.ReviewNote,
            item.ReviewedAt,
            evidence = item.InventoryItemEvidence.OrderByDescending(evidence => evidence.UploadedAt).Select(evidence => new
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
    public async Task<ActionResult<object>> CreateSession(
        [FromBody] CreateInventoryDto dto,
        CancellationToken cancellationToken)
    {
        dto.Name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Tên đợt kiểm kê là bắt buộc." });
        }

        if (dto.LocationNodeId.HasValue && !await _context.LocationNodes.AnyAsync(
                node => node.Id == dto.LocationNodeId.Value && node.IsActive,
                cancellationToken))
        {
            return BadRequest(new { message = "Vị trí kiểm kê không tồn tại hoặc đã ngừng sử dụng." });
        }
        if (dto.AssetCategoryId.HasValue && !await _context.AssetCategories.AnyAsync(
                category => category.Id == dto.AssetCategoryId.Value,
                cancellationToken))
        {
            return BadRequest(new { message = "Danh mục kiểm kê không tồn tại." });
        }

        var equipmentQuery = _context.Equipments
            .AsNoTracking()
            .Where(item => item.Status != EquipmentStatuses.Borrowed
                && item.Status != EquipmentStatuses.BorrowPending
                && !_context.BorrowRecords.Any(record =>
                    (record.Status == BorrowStatuses.Approved || record.Status == BorrowStatuses.Borrowed)
                    && record.Details.Any(detail => detail.EquipmentId == item.Id)))
            .AsQueryable();
        if (dto.LocationNodeId.HasValue)
        {
            equipmentQuery = equipmentQuery.Where(item => item.LocationNodeId == dto.LocationNodeId.Value);
        }
        if (dto.AssetCategoryId.HasValue)
        {
            equipmentQuery = equipmentQuery.Where(item => item.AssetCategoryId == dto.AssetCategoryId.Value);
        }

        var equipment = await equipmentQuery
            .Select(item => new
            {
                item.Id,
                item.LocationNodeId,
                locationName = item.LocationNode!.Name
            })
            .ToListAsync(cancellationToken);
        if (equipment.Count == 0)
        {
            return BadRequest(new { message = "Phạm vi kiểm kê không có tài sản định danh." });
        }

        var session = new InventorySession
        {
            Code = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..28],
            Name = dto.Name,
            LocationNodeId = dto.LocationNodeId,
            AssetCategoryId = dto.AssetCategoryId,
            CreatedByUserId = GetCurrentUserId(),
            Items = equipment.Select(item => new InventoryItem
            {
                EquipmentId = item.Id,
                ExpectedLocationNodeId = item.LocationNodeId,
                ExpectedLocationName = item.locationName ?? string.Empty,
                Status = InventoryItemStatuses.Pending
            }).ToList()
        };
        _context.InventorySessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        await _notificationService.NotifyManagersAsync(
            "INVENTORY_CREATED",
            "Đã tạo đợt kiểm kê",
            $"Đợt kiểm kê {session.Code} gồm {equipment.Count} tài sản.",
            $"/dashboard/inventory?session={session.Id}",
            cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Create",
            nameof(InventorySession),
            session.Id,
            new { session.Code, ItemCount = equipment.Count },
            cancellationToken);
        return Ok(new { session.Id, session.Code, message = "Đã tạo đợt kiểm kê." });
    }

    [HttpPost("{id:int}/scan")]
    public async Task<IActionResult> Scan(
        int id,
        [FromBody] ScanInventoryDto dto,
        CancellationToken cancellationToken)
    {
        var token = dto.QrToken.Trim();
        dto.Note = dto.Note.Trim();
        if (string.IsNullOrWhiteSpace(token) || dto.Note.Length > 2000)
        {
            return BadRequest(new { message = "QR token và ghi chú không hợp lệ." });
        }

        var requestedStatus = string.IsNullOrWhiteSpace(dto.Status)
            ? InventoryItemStatuses.Found
            : dto.Status.Trim();
        if (requestedStatus is not (InventoryItemStatuses.Found or InventoryItemStatuses.Damaged))
        {
            return BadRequest(new { message = "Kết quả kiểm kê không hợp lệ." });
        }

        var session = await _context.InventorySessions
            .Include(item => item.Items)
                .ThenInclude(item => item.Equipment)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        }
        if (session.Status != InventoryStatuses.Open)
        {
            return Conflict(new { message = "Đợt kiểm kê đã kết thúc." });
        }

        var inventoryItem = session.Items.SingleOrDefault(item => item.Equipment!.QrToken == token);
        if (inventoryItem is null)
        {
            return BadRequest(new { message = "QR không thuộc phạm vi của đợt kiểm kê." });
        }
        if (inventoryItem.Equipment!.Status is EquipmentStatuses.Borrowed or EquipmentStatuses.BorrowPending)
        {
            return Conflict(new { message = "Tài sản đang trong quy trình mượn và không được kiểm kê tại kho." });
        }

        inventoryItem.Status = requestedStatus;
        if (dto.LocationNodeId.HasValue && dto.LocationNodeId.Value != inventoryItem.ExpectedLocationNodeId)
        {
            inventoryItem.Status = InventoryItemStatuses.WrongLocation;
        }
        inventoryItem.ActualLocationNodeId = dto.LocationNodeId;
        inventoryItem.ScannedAt = DateTime.UtcNow;
        inventoryItem.ScannedByUserId = GetCurrentUserId();
        inventoryItem.Note = dto.Note;
        inventoryItem.ReviewedAt = inventoryItem.Status == InventoryItemStatuses.Found
            ? inventoryItem.ScannedAt
            : null;
        inventoryItem.ReviewedByUserId = inventoryItem.Status == InventoryItemStatuses.Found
            ? inventoryItem.ScannedByUserId
            : null;
        inventoryItem.ReviewResolution = inventoryItem.Status == InventoryItemStatuses.Found
            ? InventoryReviewResolutions.ConfirmedFound
            : string.Empty;
        inventoryItem.ReviewNote = string.Empty;
        inventoryItem.Equipment!.LastInventoryAt = inventoryItem.ScannedAt;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Scan",
            nameof(InventoryItem),
            inventoryItem.Id,
            new { sessionId = id, inventoryItem.EquipmentId, inventoryItem.Status },
            cancellationToken);
        return Ok(new { message = "Đã ghi nhận kết quả quét QR.", inventoryItem.Status });
    }

    [HttpPost("{id:int}/start-review")]
    public async Task<IActionResult> StartReview(int id, CancellationToken cancellationToken)
    {
        var session = await _context.InventorySessions
            .Include(item => item.Items)
                .ThenInclude(item => item.Equipment)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        }
        if (session.Status != InventoryStatuses.Open)
        {
            return Conflict(new { message = "Chỉ đợt đang kiểm kê mới được chuyển sang đối soát." });
        }

        var activeBorrowedEquipmentIds = await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => record.Status == BorrowStatuses.Approved || record.Status == BorrowStatuses.Borrowed)
            .SelectMany(record => record.Details.Select(detail => detail.EquipmentId))
            .Distinct()
            .ToListAsync(cancellationToken);
        var excludedItems = session.Items
            .Where(item => item.Equipment?.Status is EquipmentStatuses.Borrowed or EquipmentStatuses.BorrowPending
                || activeBorrowedEquipmentIds.Contains(item.EquipmentId))
            .ToList();
        _context.InventoryItems.RemoveRange(excludedItems);

        var activeItems = session.Items.Except(excludedItems).ToList();
        var now = DateTime.UtcNow;
        var reviewerId = GetCurrentUserId();
        foreach (var item in activeItems)
        {
            if (item.Status == InventoryItemStatuses.Pending)
            {
                item.Status = InventoryItemStatuses.Missing;
                item.ReviewedAt = null;
                item.ReviewedByUserId = null;
                item.ReviewResolution = string.Empty;
            }
            else if (item.Status == InventoryItemStatuses.Found)
            {
                item.ReviewedAt ??= now;
                item.ReviewedByUserId ??= reviewerId;
                item.ReviewResolution = InventoryReviewResolutions.ConfirmedFound;
            }
        }
        session.Status = InventoryStatuses.Reviewing;
        await _context.SaveChangesAsync(cancellationToken);
        await _notificationService.NotifyManagersAsync(
            "INVENTORY_REVIEWING",
            "Đợt kiểm kê chờ đối soát",
            $"Đợt kiểm kê #{id} có {activeItems.Count(item => item.Status != InventoryItemStatuses.Found)} chênh lệch cần duyệt; đã loại {excludedItems.Count} tài sản đang mượn.",
            $"/dashboard/inventory?session={id}",
            cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "StartReview",
            nameof(InventorySession),
            id,
            new
            {
                Missing = activeItems.Count(item => item.Status == InventoryItemStatuses.Missing),
                Discrepancies = activeItems.Count(item => item.Status != InventoryItemStatuses.Found),
                ExcludedBorrowed = excludedItems.Count
            },
            cancellationToken);
        return Ok(new
        {
            message = "Đã khóa quét và chuyển sang đối soát chênh lệch.",
            excludedBorrowed = excludedItems.Count
        });
    }

    [HttpPut("{sessionId:int}/items/{itemId:int}/review")]
    public async Task<IActionResult> ReviewItem(
        int sessionId,
        int itemId,
        [FromBody] ReviewInventoryItemDto dto,
        CancellationToken cancellationToken)
    {
        dto.Resolution = dto.Resolution.Trim().ToUpperInvariant();
        dto.Note = dto.Note.Trim();
        var item = await _context.InventoryItems
            .Include(value => value.InventorySession)
            .Include(value => value.Equipment)
            .Include(value => value.ActualLocationNode)
            .SingleOrDefaultAsync(value => value.Id == itemId
                && value.InventorySessionId == sessionId, cancellationToken);
        if (item?.InventorySession is null)
            return NotFound(new { message = "Không tìm thấy tài sản trong đợt kiểm kê." });
        if (item.InventorySession.Status != InventoryStatuses.Reviewing)
            return Conflict(new { message = "Đợt kiểm kê chưa ở bước đối soát." });
        if (item.Equipment is null)
            return Conflict(new { message = "Tài sản không còn tồn tại." });

        var isInActiveBorrow = item.Equipment.Status is EquipmentStatuses.Borrowed or EquipmentStatuses.BorrowPending
            || await _context.BorrowRecords.AsNoTracking().AnyAsync(record =>
                (record.Status == BorrowStatuses.Approved || record.Status == BorrowStatuses.Borrowed)
                && record.Details.Any(detail => detail.EquipmentId == item.EquipmentId),
                cancellationToken);
        if (isInActiveBorrow)
        {
            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(new { message = "Tài sản đã chuyển sang quy trình mượn nên được loại khỏi đợt kiểm kê." });
        }

        var validResolution = item.Status switch
        {
            InventoryItemStatuses.Found => dto.Resolution == InventoryReviewResolutions.ConfirmedFound,
            InventoryItemStatuses.WrongLocation => dto.Resolution is InventoryReviewResolutions.UpdateLocation
                or InventoryReviewResolutions.KeepRecordedLocation,
            InventoryItemStatuses.Damaged => dto.Resolution == InventoryReviewResolutions.MarkDamaged,
            InventoryItemStatuses.Missing => dto.Resolution == InventoryReviewResolutions.MarkMissing,
            _ => false
        };
        if (!validResolution)
            return BadRequest(new { message = "Cách xử lý không phù hợp với chênh lệch kiểm kê." });
        if (dto.Resolution == InventoryReviewResolutions.UpdateLocation && item.ActualLocationNodeId is null)
            return BadRequest(new { message = "Chưa có vị trí thực tế để cập nhật tài sản." });
        if (dto.Resolution == InventoryReviewResolutions.KeepRecordedLocation && string.IsNullOrWhiteSpace(dto.Note))
            return BadRequest(new { message = "Cần ghi rõ lý do giữ nguyên vị trí trên hệ thống." });

        switch (dto.Resolution)
        {
            case InventoryReviewResolutions.UpdateLocation:
                _context.EquipmentLocationHistories.Add(new EquipmentLocationHistory
                {
                    EquipmentId = item.Equipment.Id,
                    FromLocationNodeId = item.Equipment.LocationNodeId,
                    ToLocationNodeId = item.ActualLocationNodeId,
                    FromLocationName = item.Equipment.Location,
                    ToLocationName = item.ActualLocationNode?.Name ?? item.Equipment.Location,
                    Reason = string.IsNullOrWhiteSpace(dto.Note)
                        ? $"Điều chỉnh theo đợt kiểm kê {item.InventorySession.Code}."
                        : dto.Note,
                    ChangedByUserId = GetCurrentUserId(),
                    ChangedAt = DateTime.UtcNow
                });
                item.Equipment.LocationNodeId = item.ActualLocationNodeId;
                item.Equipment.Location = item.ActualLocationNode?.Name ?? item.Equipment.Location;
                break;
            case InventoryReviewResolutions.MarkDamaged:
                item.Equipment.Status = EquipmentStatuses.Broken;
                break;
            case InventoryReviewResolutions.MarkMissing:
                item.Equipment.Status = EquipmentStatuses.Missing;
                break;
        }

        item.ReviewResolution = dto.Resolution;
        item.ReviewNote = dto.Note;
        item.ReviewedAt = DateTime.UtcNow;
        item.ReviewedByUserId = GetCurrentUserId();
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Review",
            nameof(InventoryItem),
            item.Id,
            new { item.InventorySessionId, item.EquipmentId, item.Status, item.ReviewResolution },
            cancellationToken);
        return Ok(new { message = "Đã duyệt chênh lệch và đồng bộ trạng thái tài sản." });
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, CancellationToken cancellationToken)
    {
        var session = await _context.InventorySessions
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (session is null)
            return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        if (session.Status != InventoryStatuses.Reviewing)
            return Conflict(new { message = "Phải khóa quét và đối soát chênh lệch trước khi kết thúc." });

        var unreviewed = session.Items.Count(item =>
            item.Status != InventoryItemStatuses.Found && item.ReviewedAt == null);
        if (unreviewed > 0)
            return Conflict(new { message = $"Còn {unreviewed} chênh lệch chưa được xử lý." });

        session.Status = InventoryStatuses.Completed;
        session.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _notificationService.NotifyManagersAsync(
            "INVENTORY_COMPLETED",
            "Đợt kiểm kê đã kết thúc",
            $"Đợt kiểm kê #{id} đã hoàn tất đối soát.",
            $"/dashboard/inventory?session={id}",
            cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Complete",
            nameof(InventorySession),
            id,
            new { Discrepancies = session.Items.Count(item => item.Status != InventoryItemStatuses.Found) },
            cancellationToken);
        return Ok(new { message = "Đã kết thúc đợt kiểm kê sau khi đối soát đầy đủ." });
    }

    public sealed class UploadEvidenceDto
    {
        [Required] public IFormFile? File { get; set; }
        [Required, MaxLength(50)] public string EvidenceType { get; set; } = "PHOTO";
    }

    [HttpPost("{sessionId:int}/items/{itemId:int}/evidence")]
    [EnableRateLimiting("sensitive")]
    [RequestSizeLimit(11_000_000)]
    public async Task<IActionResult> UploadEvidence(
        int sessionId,
        int itemId,
        [FromForm] UploadEvidenceDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.File is null) return BadRequest(new { message = "Vui lòng chọn file minh chứng." });
        dto.EvidenceType = dto.EvidenceType.Trim().ToUpperInvariant();
        if (dto.EvidenceType is not ("PHOTO" or "DOCUMENT"))
            return BadRequest(new { message = "Loại minh chứng kiểm kê không hợp lệ." });

        var item = await _context.InventoryItems
            .Include(value => value.InventorySession)
            .SingleOrDefaultAsync(value => value.Id == itemId
                && value.InventorySessionId == sessionId, cancellationToken);
        if (item is null) return NotFound(new { message = "Không tìm thấy tài sản trong đợt kiểm kê." });

        StoredFile stored;
        try
        {
            stored = await _fileStorage.SaveAsync(
                dto.File,
                "inventories",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".doc", ".docx" },
                _configuration.GetValue("Uploads:MaxEvidenceFileBytes", 10 * 1024 * 1024L),
                cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        var evidence = new InventoryEvidence
        {
            InventoryItemId = item.Id,
            EvidenceType = dto.EvidenceType,
            OriginalFileName = stored.OriginalFileName,
            StoredPath = stored.StoredPath,
            ContentType = stored.ContentType,
            FileSize = stored.Length,
            UploadedByUserId = GetCurrentUserId()
        };
        _context.InventoryEvidence.Add(evidence);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "UploadEvidence", nameof(InventoryItem), item.Id,
            new { evidence.Id, evidence.EvidenceType }, cancellationToken);
        return Ok(new { evidence.Id, evidence.OriginalFileName, message = "Đã lưu minh chứng kiểm kê." });
    }

    [HttpGet("{sessionId:int}/items/{itemId:int}/evidence/{evidenceId:long}")]
    public async Task<IActionResult> DownloadEvidence(
        int sessionId,
        int itemId,
        long evidenceId,
        CancellationToken cancellationToken)
    {
        var evidence = await _context.InventoryEvidence.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == evidenceId
                && value.InventoryItemId == itemId
                && value.InventoryItem!.InventorySessionId == sessionId,
                cancellationToken);
        if (evidence is null) return NotFound();
        var stream = await _fileStorage.OpenReadAsync(evidence.StoredPath, cancellationToken);
        if (stream is null) return NotFound();
        return File(stream, evidence.ContentType, evidence.OriginalFileName, enableRangeProcessing: true);
    }

    [HttpGet("{id:int}/export.xlsx")]
    public async Task<IActionResult> ExportExcel(int id, CancellationToken cancellationToken)
    {
        var session = await LoadReportSession(id, cancellationToken);
        if (session is null) return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        ExcelPackage.License.SetNonCommercialOrganization("LabManagement Educational Project");
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("ChenhLech");
        var headers = new[]
        {
            "Mã tài sản", "Tên tài sản", "Số seri", "Vị trí dự kiến", "Trạng thái",
            "Thời gian quét", "Ghi chú", "Số lượng sổ sách", "Số lượng thực tế", "Chênh lệch"
        };
        for (var index = 0; index < headers.Length; index++)
        {
            sheet.Cells[1, index + 1].Value = headers[index];
            sheet.Cells[1, index + 1].Style.Font.Bold = true;
        }
        for (var index = 0; index < session.Items.Count; index++)
        {
            var item = session.Items.ToList()[index];
            var row = index + 2;
            WriteExcelText(sheet, row, 1, item.Equipment?.AssetCode);
            WriteExcelText(sheet, row, 2, item.Equipment?.Name);
            WriteExcelText(sheet, row, 3, item.Equipment?.Serial);
            WriteExcelText(sheet, row, 4, item.ExpectedLocationName);
            WriteExcelText(sheet, row, 5, InventoryStatusLabel(item.Status));
            WriteExcelText(sheet, row, 6, item.ScannedAt?.ToString("dd/MM/yyyy HH:mm"));
            WriteExcelText(sheet, row, 7, item.Note);
            WriteExcelNumber(sheet, row, 8, 1);
            WriteExcelNumber(sheet, row, 9, InventoryActualQuantity(item.Status));
            WriteExcelNumber(sheet, row, 10, InventoryQuantityDifference(item.Status));
        }
        sheet.Cells[sheet.Dimension?.Address ?? "A1"].AutoFitColumns();
        var fileContents = await package.GetAsByteArrayAsync(cancellationToken);
        return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"KiemKe_{session.Code}.xlsx");
    }

    [HttpGet("{id:int}/export.pdf")]
    public async Task<IActionResult> ExportPdf(int id, CancellationToken cancellationToken)
    {
        var session = await LoadReportSession(id, cancellationToken);
        if (session is null) return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        QuestPDF.Settings.License = LicenseType.Community;
        var document = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(24);
            page.DefaultTextStyle(style => style.FontSize(8));
            page.Header().Column(column =>
            {
                column.Item().Text("BÁO CÁO KIỂM KÊ TÀI SẢN").Bold().FontSize(16);
                column.Item().Text($"{session.Code} — {session.Name} — {InventoryStatusLabel(session.Status)}");
            });
            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(28); columns.RelativeColumn(1.4f); columns.RelativeColumn(2);
                    columns.RelativeColumn(1.4f); columns.ConstantColumn(52); columns.ConstantColumn(52);
                    columns.ConstantColumn(50); columns.RelativeColumn(1.2f); columns.RelativeColumn(1.8f);
                });
                table.Header(header =>
                {
                    foreach (var title in new[]
                    {
                        "STT", "Mã tài sản", "Tên tài sản", "Vị trí dự kiến", "SL sổ sách", "SL thực tế",
                        "Chênh lệch", "Kết quả", "Ghi chú"
                    })
                        header.Cell().Element(HeaderCell).Text(title);
                });
                foreach (var (item, index) in session.Items.Select((value, index) => (value, index)))
                {
                    table.Cell().Element(BodyCell).Text((index + 1).ToString());
                    table.Cell().Element(BodyCell).Text(item.Equipment?.AssetCode ?? "");
                    table.Cell().Element(BodyCell).Text(item.Equipment?.Name ?? "");
                    table.Cell().Element(BodyCell).Text(item.ExpectedLocationName);
                    table.Cell().Element(BodyCell).Text(QuantityText(1));
                    table.Cell().Element(BodyCell).Text(QuantityText(InventoryActualQuantity(item.Status)));
                    table.Cell().Element(BodyCell).Text(QuantityText(InventoryQuantityDifference(item.Status)));
                    table.Cell().Element(BodyCell).Text(InventoryStatusLabel(item.Status));
                    table.Cell().Element(BodyCell).Text(item.Note);
                }
            });
            page.Footer().AlignCenter().Text(text => { text.Span("LabManagement — Trang "); text.CurrentPageNumber(); });
        }));
        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return File(stream.ToArray(), "application/pdf", $"KiemKe_{session.Code}.pdf");

        static IContainer HeaderCell(IContainer container) => container.Background(Colors.Blue.Darken2)
            .Padding(4).DefaultTextStyle(style => style.FontColor(Colors.White).Bold());
        static IContainer BodyCell(IContainer container) => container.BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2).Padding(4);
    }

    private async Task<InventorySession?> LoadReportSession(int id, CancellationToken cancellationToken)
        => await _context.InventorySessions.AsNoTracking()
            .Include(session => session.Items).ThenInclude(item => item.Equipment)
            .SingleOrDefaultAsync(session => session.Id == id, cancellationToken);

    private static void WriteExcelText(ExcelWorksheet sheet, int row, int column, string? value)
    {
        var text = value ?? string.Empty;
        sheet.Cells[row, column].Value = text.Length > 0 && text[0] is '=' or '+' or '-' or '@'
            ? "'" + text
            : text;
    }

    private static void WriteExcelNumber(ExcelWorksheet sheet, int row, int column, int? value)
    {
        sheet.Cells[row, column].Value = value;
    }

    private static int? InventoryActualQuantity(string status) => status switch
    {
        InventoryItemStatuses.Found or InventoryItemStatuses.WrongLocation or InventoryItemStatuses.Damaged => 1,
        InventoryItemStatuses.Missing => 0,
        InventoryItemStatuses.Pending => null,
        _ => null
    };

    private static int? InventoryQuantityDifference(string status)
    {
        var actualQuantity = InventoryActualQuantity(status);
        return actualQuantity.HasValue ? actualQuantity.Value - 1 : null;
    }

    private static string QuantityText(int? value) => value?.ToString() ?? "—";

    private static string InventoryStatusLabel(string status) => status switch
    {
        InventoryStatuses.Open => "Đang kiểm kê",
        InventoryStatuses.Reviewing => "Đang đối soát",
        InventoryStatuses.Completed => "Đã kết thúc",
        InventoryItemStatuses.Found => "Đã tìm thấy",
        InventoryItemStatuses.WrongLocation => "Sai vị trí",
        InventoryItemStatuses.Damaged => "Hư hỏng",
        InventoryItemStatuses.Missing => "Thất lạc",
        InventoryItemStatuses.Pending => "Chưa kiểm kê",
        _ => status
    };

    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
