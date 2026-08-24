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
[Authorize]
public class ConsumableController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public ConsumableController(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public sealed class ConsumableDto
    {
        [MaxLength(100)] public string Code { get; set; } = string.Empty;
        [Required, MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Unit { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, int.MaxValue)]
        public int MinQuantity { get; set; }

        [MaxLength(255)]
        public string ResponsiblePerson { get; set; } = string.Empty;

        public int? AssetCategoryId { get; set; }
        public DateTime? EntryDate { get; set; }

        [MaxLength(100)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [MaxLength(255)] public string Supplier { get; set; } = string.Empty;
        [Range(0, double.MaxValue)] public decimal? UnitCost { get; set; }
        [MaxLength(255)] public string StorageLocation { get; set; } = string.Empty;
        [MaxLength(100)] public string LotNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetConsumables(
        CancellationToken cancellationToken)
    {
        var assets = await _context.Consumables
            .AsNoTracking()
            .Include(item => item.AssetCategory)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(assets.Select(item => new
        {
            item.Id,
            item.Code,
            item.Name,
            item.Unit,
            item.Quantity,
            item.MinQuantity,
            item.ResponsiblePerson,
            item.AssetCategoryId,
            CategoryName = item.AssetCategory?.Name,
            item.EntryDate,
            item.InvoiceNumber,
            item.Supplier,
            item.UnitCost,
            item.StorageLocation,
            item.LotNumber,
            item.ExpiryDate,
            item.CreatedAt
        }));
    }

    [HttpGet("{id:int}/transactions")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult<IEnumerable<object>>> GetTransactions(
        int id,
        CancellationToken cancellationToken)
    {
        var exists = await _context.Consumables.AnyAsync(
            item => item.Id == id,
            cancellationToken);
        if (!exists)
        {
            return NotFound();
        }

        var transactions = await _context.ConsumableTransactions
            .AsNoTracking()
            .Include(transaction => transaction.User)
            .Where(transaction => transaction.ConsumableId == id)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(transactions.Select(transaction => new
        {
            transaction.Id,
            transaction.ConsumableId,
            transaction.Type,
            transaction.Quantity,
            transaction.BeforeQuantity,
            transaction.AfterQuantity,
            transaction.Reason,
            transaction.UserId,
            Username = transaction.User?.Username,
            transaction.CreatedAt
        }));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult<Consumable>> PostConsumable(
        [FromBody] ConsumableDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await ValidateDtoAsync(dto, null, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var consumable = new Consumable
        {
            Name = dto.Name.Trim(),
            Code = await ResolveCodeAsync(dto.Code, null, cancellationToken),
            Unit = dto.Unit.Trim(),
            Quantity = dto.Quantity,
            MinQuantity = dto.MinQuantity,
            ResponsiblePerson = dto.ResponsiblePerson.Trim(),
            AssetCategoryId = dto.AssetCategoryId,
            EntryDate = dto.EntryDate,
            InvoiceNumber = dto.InvoiceNumber.Trim(),
            Supplier = dto.Supplier.Trim(),
            UnitCost = dto.UnitCost,
            StorageLocation = dto.StorageLocation.Trim(),
            LotNumber = dto.LotNumber.Trim(),
            ExpiryDate = dto.ExpiryDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.Consumables.Add(consumable);
        await _context.SaveChangesAsync(cancellationToken);

        if (consumable.Quantity > 0)
        {
            _context.ConsumableTransactions.Add(new ConsumableTransaction
            {
                ConsumableId = consumable.Id,
                Type = "Nhập kho",
                Quantity = consumable.Quantity,
                BeforeQuantity = 0,
                AfterQuantity = consumable.Quantity,
                Reason = "Tạo vật tư ban đầu",
                UserId = GetCurrentUserIdOrNull(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        await _auditService.WriteAsync(
            HttpContext,
            "Create",
            nameof(Consumable),
            consumable.Id,
            new { consumable.Name, consumable.Quantity },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(consumable);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> PutConsumable(
        int id,
        [FromBody] ConsumableDto dto,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var existing = await _context.Consumables.FindAsync(
            new object[] { id },
            cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var validationResult = await ValidateDtoAsync(dto, id, cancellationToken);
        if (validationResult is not null)
        {
            return validationResult;
        }

        var before = SnapshotConsumable(existing);
        var beforeQuantity = existing.Quantity;

        existing.Name = dto.Name.Trim();
        existing.Code = await ResolveCodeAsync(dto.Code, id, cancellationToken);
        existing.Unit = dto.Unit.Trim();
        existing.Quantity = dto.Quantity;
        existing.MinQuantity = dto.MinQuantity;
        existing.ResponsiblePerson = dto.ResponsiblePerson.Trim();
        existing.AssetCategoryId = dto.AssetCategoryId;
        existing.EntryDate = dto.EntryDate;
        existing.InvoiceNumber = dto.InvoiceNumber.Trim();
        existing.Supplier = dto.Supplier.Trim();
        existing.UnitCost = dto.UnitCost;
        existing.StorageLocation = dto.StorageLocation.Trim();
        existing.LotNumber = dto.LotNumber.Trim();
        existing.ExpiryDate = dto.ExpiryDate;

        if (beforeQuantity != existing.Quantity)
        {
            _context.ConsumableTransactions.Add(new ConsumableTransaction
            {
                ConsumableId = existing.Id,
                Type = "Điều chỉnh",
                Quantity = Math.Abs(existing.Quantity - beforeQuantity),
                BeforeQuantity = beforeQuantity,
                AfterQuantity = existing.Quantity,
                Reason = "Điều chỉnh số lượng trực tiếp",
                UserId = GetCurrentUserIdOrNull(),
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Update",
            nameof(Consumable),
            id,
            new
            {
                Before = before,
                After = SnapshotConsumable(existing)
            },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteConsumable(
        int id,
        CancellationToken cancellationToken)
    {
        var consumable = await _context.Consumables.FindAsync(
            new object[] { id },
            cancellationToken);
        if (consumable is null)
        {
            return NotFound();
        }

        if (await _context.ConsumableRequests.AnyAsync(
                request => request.ConsumableId == id,
                cancellationToken))
        {
            return BadRequest(new { message = "Không thể xóa vật tư đã có lịch sử cấp phát." });
        }

        _context.Consumables.Remove(consumable);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Delete",
            nameof(Consumable),
            id,
            new { consumable.Name },
            cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult?> ValidateDtoAsync(
        ConsumableDto dto,
        int? currentId,
        CancellationToken cancellationToken)
    {
        dto.Name = dto.Name.Trim();
        dto.Unit = dto.Unit.Trim();
        dto.Code = dto.Code.Trim();
        dto.Supplier = dto.Supplier.Trim();
        dto.StorageLocation = dto.StorageLocation.Trim();
        dto.LotNumber = dto.LotNumber.Trim();
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Unit))
        {
            return BadRequest(new { message = "Tên vật tư và đơn vị tính là bắt buộc." });
        }

        if (dto.AssetCategoryId.HasValue
            && !await _context.AssetCategories.AnyAsync(
                category => category.Id == dto.AssetCategoryId.Value,
                cancellationToken))
        {
            return BadRequest(new { message = "Danh mục không tồn tại." });
        }

        if (!string.IsNullOrWhiteSpace(dto.Code)
            && await _context.Consumables.AnyAsync(item => item.Code == dto.Code
                && (!currentId.HasValue || item.Id != currentId.Value), cancellationToken))
        {
            return Conflict(new { message = "Mã vật tư đã tồn tại." });
        }

        var duplicate = await _context.Consumables.AnyAsync(
            item => item.Name == dto.Name
                && item.Unit == dto.Unit
                && (!currentId.HasValue || item.Id != currentId.Value),
            cancellationToken);
        return duplicate
            ? Conflict(new { message = "Vật tư cùng tên và đơn vị đã tồn tại." })
            : null;
    }

    private static object SnapshotConsumable(Consumable consumable)
    {
        return new
        {
            consumable.Name,
            consumable.Code,
            consumable.Unit,
            consumable.Quantity,
            consumable.MinQuantity,
            consumable.ResponsiblePerson,
            consumable.AssetCategoryId,
            consumable.EntryDate,
            consumable.InvoiceNumber
            ,consumable.Supplier,
            consumable.UnitCost,
            consumable.StorageLocation,
            consumable.LotNumber,
            consumable.ExpiryDate
        };
    }

    private async Task<string> ResolveCodeAsync(string value, int? currentId, CancellationToken cancellationToken)
    {
        var code = value.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            do
            {
                code = $"VT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
            }
            while (await _context.Consumables.AnyAsync(item => item.Code == code, cancellationToken));
        }

        if (await _context.Consumables.AnyAsync(item => item.Code == code
            && (!currentId.HasValue || item.Id != currentId.Value), cancellationToken))
        {
            throw new InvalidOperationException("Mã vật tư đã tồn tại.");
        }

        return code;
    }

    private int? GetCurrentUserIdOrNull()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
    }
}
