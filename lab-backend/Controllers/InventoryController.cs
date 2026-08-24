using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = Roles.Managers)]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public InventoryController(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
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
            total = session.Items.Count,
            found = session.Items.Count(item => item.Status == InventoryItemStatuses.Found),
            wrongLocation = session.Items.Count(item => item.Status == InventoryItemStatuses.WrongLocation),
            damaged = session.Items.Count(item => item.Status == InventoryItemStatuses.Damaged),
            missing = session.Items.Count(item => item.Status == InventoryItemStatuses.Missing),
            pending = session.Items.Count(item => item.Status == InventoryItemStatuses.Pending)
        }));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetSession(int id, CancellationToken cancellationToken)
    {
        var session = await _context.InventorySessions
            .AsNoTracking()
            .Include(item => item.LocationNode)
            .Include(item => item.AssetCategory)
            .Include(item => item.Items)
                .ThenInclude(item => item.Equipment)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        }

        return Ok(new
        {
            session.Id,
            session.Code,
            session.Name,
            session.Status,
            session.StartedAt,
            session.CompletedAt,
            locationName = session.LocationNode?.Name,
            categoryName = session.AssetCategory?.Name,
            items = session.Items.OrderBy(item => item.Equipment!.Name).Select(item => new
            {
                item.Id,
                item.EquipmentId,
                assetCode = item.Equipment!.AssetCode,
                qrToken = item.Equipment.QrToken,
                equipmentName = item.Equipment.Name,
                serial = item.Equipment.Serial,
                expectedLocation = item.ExpectedLocationName,
                item.Status,
                item.ScannedAt,
                item.Note
            })
        });
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

        var equipmentQuery = _context.Equipments.AsNoTracking().AsQueryable();
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

        inventoryItem.Status = requestedStatus;
        if (dto.LocationNodeId.HasValue && dto.LocationNodeId.Value != inventoryItem.ExpectedLocationNodeId)
        {
            inventoryItem.Status = InventoryItemStatuses.WrongLocation;
        }
        inventoryItem.ScannedAt = DateTime.UtcNow;
        inventoryItem.ScannedByUserId = GetCurrentUserId();
        inventoryItem.Note = dto.Note;
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

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, CancellationToken cancellationToken)
    {
        var session = await _context.InventorySessions
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Không tìm thấy đợt kiểm kê." });
        }
        if (session.Status != InventoryStatuses.Open)
        {
            return Conflict(new { message = "Đợt kiểm kê đã kết thúc." });
        }

        foreach (var item in session.Items.Where(item => item.Status == InventoryItemStatuses.Pending))
        {
            item.Status = InventoryItemStatuses.Missing;
        }
        session.Status = InventoryStatuses.Completed;
        session.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Complete",
            nameof(InventorySession),
            id,
            new { Missing = session.Items.Count(item => item.Status == InventoryItemStatuses.Missing) },
            cancellationToken);
        return Ok(new { message = "Đã kết thúc đợt kiểm kê." });
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
