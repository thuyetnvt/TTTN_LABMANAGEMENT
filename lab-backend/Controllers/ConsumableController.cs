using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LabManagementAPI.Data;
using LabManagementAPI.Dtos;
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

    public sealed class ConsumableLotDto
    {
        [Required, MaxLength(100)] public string LotNumber { get; set; } = string.Empty;
        [Range(0, int.MaxValue)] public int Quantity { get; set; }
        public DateTime? EntryDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        [MaxLength(255)] public string Supplier { get; set; } = string.Empty;
        [MaxLength(100)] public string InvoiceNumber { get; set; } = string.Empty;
        [Range(0, double.MaxValue)] public decimal? UnitCost { get; set; }
        [MaxLength(255)] public string StorageLocation { get; set; } = string.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetConsumables(
        CancellationToken cancellationToken)
    {
        var assets = await _context.Consumables
            .AsNoTracking()
            .Include(item => item.AssetCategory)
            .Include(item => item.Lots)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        var isManager = User.IsInRole(Roles.Admin)
            || User.IsInRole(Roles.LabHead)
            || User.IsInRole(Roles.DeputyLabHead);
        if (!isManager)
        {
            return Ok(assets.Select(ToBorrowerDto));
        }

        return Ok(assets.Select(ToManagerDto));
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetConsumablesPaged(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        var query = _context.Consumables
            .AsNoTracking()
            .Include(item => item.AssetCategory)
            .Include(item => item.Lots)
            .AsQueryable();
        var search = paging.NormalizedSearch;
        if (search.Length > 0)
        {
            query = query.Where(item =>
                item.Name.Contains(search)
                || item.Code.Contains(search)
                || item.Unit.Contains(search)
                || (item.AssetCategory != null && item.AssetCategory.Name.Contains(search)));
        }
        if (paging.CategoryId.HasValue)
        {
            query = query.Where(item => item.AssetCategoryId == paging.CategoryId.Value);
        }
        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            var status = paging.Status.Trim().ToUpperInvariant();
            if (status == "LOW_STOCK")
            {
                query = query.Where(item => item.Quantity - item.ReservedQuantity <= item.MinQuantity);
            }
            else if (status == "AVAILABLE")
            {
                query = query.Where(item => item.Quantity - item.ReservedQuantity > item.MinQuantity);
            }
        }

        var page = await query
            .OrderByDescending(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToPagedResultAsync(paging, cancellationToken);
        var isManager = User.IsInRole(Roles.Admin)
            || User.IsInRole(Roles.LabHead)
            || User.IsInRole(Roles.DeputyLabHead);
        if (isManager)
        {
            return Ok(page.Map(ToManagerDto));
        }

        return Ok(page.Map(ToBorrowerDto));
    }

    [HttpGet("lookup")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> LookupConsumables(
        [FromQuery, MaxLength(200)] string? search = null,
        [FromQuery, Range(1, 50)] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Consumables.AsNoTracking().AsQueryable();
        var keyword = search?.Trim() ?? string.Empty;
        if (keyword.Length > 0)
        {
            query = query.Where(item => item.Name.Contains(keyword) || item.Code.Contains(keyword));
        }

        var items = await query
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Take(Math.Clamp(limit, 1, 50))
            .Select(item => new
            {
                item.Id,
                item.Code,
                item.Name,
                item.Unit,
                quantity = item.Quantity > item.ReservedQuantity ? item.Quantity - item.ReservedQuantity : 0
            })
            .ToListAsync(cancellationToken);
        return Ok(items);
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
            var lotNumber = string.IsNullOrWhiteSpace(dto.LotNumber)
                ? $"LOT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25].ToUpperInvariant()
                : dto.LotNumber.Trim();
            _context.ConsumableLots.Add(new ConsumableLot
            {
                ConsumableId = consumable.Id,
                LotNumber = lotNumber,
                InitialQuantity = consumable.Quantity,
                Quantity = consumable.Quantity,
                EntryDate = dto.EntryDate ?? DateTime.UtcNow,
                ExpiryDate = dto.ExpiryDate,
                Supplier = dto.Supplier.Trim(),
                InvoiceNumber = dto.InvoiceNumber.Trim(),
                UnitCost = dto.UnitCost,
                StorageLocation = dto.StorageLocation.Trim()
            });
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

        if (dto.Quantity != beforeQuantity)
        {
            return BadRequest(new { message = "Số lượng tồn kho được quản lý theo lô. Hãy dùng chức năng Quản lý lô để nhập hoặc điều chỉnh." });
        }

        existing.Name = dto.Name.Trim();
        existing.Code = await ResolveCodeAsync(dto.Code, id, cancellationToken);
        existing.Unit = dto.Unit.Trim();
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

    [HttpGet("{id:int}/lots")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> GetLots(int id, CancellationToken cancellationToken)
    {
        if (!await _context.Consumables.AnyAsync(item => item.Id == id, cancellationToken))
            return NotFound(new { message = "Không tìm thấy vật tư." });

        var todayStartUtc = VietnamTime.StartOfDayUtc(VietnamTime.Today());
        var lots = await _context.ConsumableLots.AsNoTracking()
            .Where(item => item.ConsumableId == id)
            .OrderBy(item => item.ExpiryDate == null)
            .ThenBy(item => item.ExpiryDate)
            .ThenBy(item => item.EntryDate)
            .Select(item => new
            {
                item.Id,
                item.ConsumableId,
                item.LotNumber,
                item.InitialQuantity,
                item.Quantity,
                item.EntryDate,
                item.ExpiryDate,
                item.Supplier,
                item.InvoiceNumber,
                item.UnitCost,
                item.StorageLocation,
                isExpired = item.ExpiryDate.HasValue && item.ExpiryDate.Value < todayStartUtc
            })
            .ToListAsync(cancellationToken);
        return Ok(lots);
    }

    [HttpPost("{id:int}/lots")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> AddLot(
        int id,
        [FromBody] ConsumableLotDto dto,
        CancellationToken cancellationToken)
    {
        NormalizeLotDto(dto);
        if (string.IsNullOrWhiteSpace(dto.LotNumber) || dto.Quantity <= 0)
            return BadRequest(new { message = "Số lô và số lượng nhập phải hợp lệ." });
        if (dto.ExpiryDate.HasValue && VietnamTime.Date(dto.ExpiryDate.Value) < VietnamTime.Today())
            return BadRequest(new { message = "Không thể nhập lô đã hết hạn sử dụng." });

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var consumable = await _context.Consumables.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (consumable is null) return NotFound(new { message = "Không tìm thấy vật tư." });
        if (await _context.ConsumableLots.AnyAsync(
            item => item.ConsumableId == id && item.LotNumber == dto.LotNumber,
            cancellationToken))
            return Conflict(new { message = "Số lô đã tồn tại cho vật tư này." });

        var before = consumable.Quantity;
        var lot = new ConsumableLot
        {
            ConsumableId = id,
            LotNumber = dto.LotNumber,
            InitialQuantity = dto.Quantity,
            Quantity = dto.Quantity,
            EntryDate = dto.EntryDate ?? DateTime.UtcNow,
            ExpiryDate = dto.ExpiryDate,
            Supplier = dto.Supplier,
            InvoiceNumber = dto.InvoiceNumber,
            UnitCost = dto.UnitCost,
            StorageLocation = dto.StorageLocation
        };
        consumable.Quantity += dto.Quantity;
        _context.ConsumableLots.Add(lot);
        _context.ConsumableTransactions.Add(new ConsumableTransaction
        {
            ConsumableId = id,
            Type = "Nhập kho",
            Quantity = dto.Quantity,
            BeforeQuantity = before,
            AfterQuantity = consumable.Quantity,
            Reason = $"Nhập lô {dto.LotNumber}",
            UserId = GetCurrentUserIdOrNull()
        });
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "CreateLot", nameof(ConsumableLot), lot.Id,
            new { lot.ConsumableId, lot.LotNumber, lot.Quantity }, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { lot.Id, message = "Đã nhập lô vật tư và cập nhật tồn kho." });
    }

    [HttpPut("{id:int}/lots/{lotId:int}")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> UpdateLot(
        int id,
        int lotId,
        [FromBody] ConsumableLotDto dto,
        CancellationToken cancellationToken)
    {
        NormalizeLotDto(dto);
        if (string.IsNullOrWhiteSpace(dto.LotNumber) || dto.Quantity < 0)
            return BadRequest(new { message = "Thông tin lô không hợp lệ." });

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var lot = await _context.ConsumableLots
            .Include(item => item.Consumable)
            .SingleOrDefaultAsync(item => item.Id == lotId && item.ConsumableId == id, cancellationToken);
        if (lot?.Consumable is null) return NotFound(new { message = "Không tìm thấy lô vật tư." });
        if (await _context.ConsumableLots.AnyAsync(item => item.ConsumableId == id
            && item.LotNumber == dto.LotNumber && item.Id != lotId, cancellationToken))
            return Conflict(new { message = "Số lô đã tồn tại cho vật tư này." });

        var delta = dto.Quantity - lot.Quantity;
        var newTotal = lot.Consumable.Quantity + delta;
        if (newTotal < lot.Consumable.ReservedQuantity)
            return Conflict(new { message = "Không thể giảm tồn kho thấp hơn số lượng đã giữ cho các yêu cầu đã duyệt." });

        var before = lot.Consumable.Quantity;
        if (delta > 0) lot.InitialQuantity += delta;
        lot.Quantity = dto.Quantity;
        lot.LotNumber = dto.LotNumber;
        lot.EntryDate = dto.EntryDate ?? lot.EntryDate;
        lot.ExpiryDate = dto.ExpiryDate;
        lot.Supplier = dto.Supplier;
        lot.InvoiceNumber = dto.InvoiceNumber;
        lot.UnitCost = dto.UnitCost;
        lot.StorageLocation = dto.StorageLocation;
        lot.Consumable.Quantity = newTotal;
        if (delta != 0)
        {
            _context.ConsumableTransactions.Add(new ConsumableTransaction
            {
                ConsumableId = id,
                Type = delta > 0 ? "Nhập kho" : "Điều chỉnh",
                Quantity = Math.Abs(delta),
                BeforeQuantity = before,
                AfterQuantity = newTotal,
                Reason = $"Điều chỉnh lô {dto.LotNumber}",
                UserId = GetCurrentUserIdOrNull()
            });
        }
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(HttpContext, "UpdateLot", nameof(ConsumableLot), lot.Id,
            new { lot.ConsumableId, lot.LotNumber, lot.Quantity }, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "Đã cập nhật lô và đồng bộ tồn kho." });
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

    private static BorrowerConsumableDto ToBorrowerDto(Consumable item)
    {
        var available = Math.Max(0, item.Quantity - item.ReservedQuantity);
        return new BorrowerConsumableDto
        {
            Id = item.Id,
            Code = item.Code,
            Name = item.Name,
            Unit = item.Unit,
            Quantity = available,
            AvailableQuantity = available,
            MinQuantity = item.MinQuantity,
            AssetCategoryId = item.AssetCategoryId,
            CategoryName = item.AssetCategory?.Name
        };
    }

    private static ManagerConsumableDto ToManagerDto(Consumable item)
    {
        return new ManagerConsumableDto
        {
            Id = item.Id,
            Code = item.Code,
            Name = item.Name,
            Unit = item.Unit,
            Quantity = item.Quantity,
            ReservedQuantity = item.ReservedQuantity,
            AvailableQuantity = Math.Max(0, item.Quantity - item.ReservedQuantity),
            MinQuantity = item.MinQuantity,
            ResponsiblePerson = item.ResponsiblePerson,
            AssetCategoryId = item.AssetCategoryId,
            CategoryName = item.AssetCategory?.Name,
            EntryDate = item.EntryDate,
            InvoiceNumber = item.InvoiceNumber,
            Supplier = item.Supplier,
            UnitCost = item.UnitCost,
            StorageLocation = item.StorageLocation,
            LotNumber = item.LotNumber,
            ExpiryDate = item.ExpiryDate,
            LotCount = item.Lots.Count,
            CreatedAt = item.CreatedAt
        };
    }

    private static void NormalizeLotDto(ConsumableLotDto dto)
    {
        dto.LotNumber = dto.LotNumber.Trim();
        dto.Supplier = dto.Supplier.Trim();
        dto.InvoiceNumber = dto.InvoiceNumber.Trim();
        dto.StorageLocation = dto.StorageLocation.Trim();
    }
}
