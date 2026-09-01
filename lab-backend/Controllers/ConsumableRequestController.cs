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
public class ConsumableRequestController : ControllerBase
{
    private const string Pending = ConsumableRequestStatuses.Pending;
    private const string Processing = ConsumableRequestStatuses.Processing;
    private const string Approved = ConsumableRequestStatuses.Approved;
    private const string HandedOver = ConsumableRequestStatuses.HandedOver;
    private const string Received = ConsumableRequestStatuses.Received;
    private const string Issued = ConsumableRequestStatuses.Issued;
    private const string Rejected = ConsumableRequestStatuses.Rejected;

    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;

    public ConsumableRequestController(
        AppDbContext context,
        INotificationService notificationService,
        IAuditService auditService)
    {
        _context = context;
        _notificationService = notificationService;
        _auditService = auditService;
    }

    public sealed class CreateConsumableRequestDto
    {
        [Range(1, int.MaxValue)]
        public int ConsumableId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class LotAllocationDto
    {
        [Range(1, int.MaxValue)] public int LotId { get; set; }
        [Range(1, int.MaxValue)] public int Quantity { get; set; }
    }

    public sealed class HandoverConsumableDto
    {
        [Required, MinLength(1)]
        public List<LotAllocationDto> Allocations { get; set; } = new();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetRequests(
        CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var userId = GetCurrentUserId();

        var query = _context.ConsumableRequests
            .AsNoTracking()
            .Include(request => request.Consumable)
                .ThenInclude(consumable => consumable!.AssetCategory)
            .Include(request => request.User)
            .Include(request => request.LotAllocations)
                .ThenInclude(allocation => allocation.ConsumableLot)
            .AsQueryable();

        if (role is Roles.Student or Roles.Teacher)
        {
            query = query.Where(request => request.UserId == userId);
        }

        var requests = await query
            .OrderByDescending(request => request.RequestDate)
            .ToListAsync(cancellationToken);

        return Ok(requests.Select(request => new
        {
            request.Id,
            request.ConsumableId,
            ConsumableName = request.Consumable?.Name,
            CategoryName = request.Consumable?.AssetCategory?.Name,
            request.UserId,
            Username = request.User?.Username,
            request.Quantity,
            request.Reason,
            request.Status,
            request.RequestDate,
            request.ApprovalDate,
            request.HandedOverAt,
            request.ReceivedAt,
            allocations = request.LotAllocations.Select(allocation => new
            {
                allocation.ConsumableLotId,
                lotNumber = allocation.ConsumableLot!.LotNumber,
                allocation.Quantity,
                allocation.ConsumableLot.ExpiryDate
            })
        }));
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetRequestsPaged(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var userId = GetCurrentUserId();
        var query = _context.ConsumableRequests
            .AsNoTracking()
            .Include(request => request.Consumable)
                .ThenInclude(consumable => consumable!.AssetCategory)
            .Include(request => request.User)
            .Include(request => request.LotAllocations)
                .ThenInclude(allocation => allocation.ConsumableLot)
            .AsQueryable();
        if (role is Roles.Student or Roles.Teacher)
        {
            query = query.Where(request => request.UserId == userId);
        }

        var search = paging.NormalizedSearch;
        if (search.Length > 0)
        {
            query = query.Where(request =>
                request.Consumable!.Name.Contains(search)
                || request.Consumable.Code.Contains(search)
                || request.User!.Username.Contains(search)
                || request.User.FullName.Contains(search)
                || request.Reason.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            var status = paging.Status.Trim();
            query = query.Where(request => request.Status == status);
        }
        if (paging.From.HasValue)
        {
            query = query.Where(request => request.RequestDate >= paging.From.Value);
        }
        if (paging.To.HasValue)
        {
            var exclusiveTo = paging.To.Value.Date.AddDays(1);
            query = query.Where(request => request.RequestDate < exclusiveTo);
        }

        var page = await query
            .OrderByDescending(request => request.RequestDate)
            .ThenByDescending(request => request.Id)
            .ToPagedResultAsync(paging, cancellationToken);
        var items = page.Items.Select(request => (object)new
        {
            request.Id,
            request.ConsumableId,
            ConsumableName = request.Consumable?.Name,
            CategoryName = request.Consumable?.AssetCategory?.Name,
            request.UserId,
            Username = request.User?.Username,
            request.Quantity,
            request.Reason,
            request.Status,
            request.RequestDate,
            request.ApprovalDate,
            request.HandedOverAt,
            request.ReceivedAt,
            allocations = request.LotAllocations.Select(allocation => new
            {
                allocation.ConsumableLotId,
                lotNumber = allocation.ConsumableLot!.LotNumber,
                allocation.Quantity,
                allocation.ConsumableLot.ExpiryDate
            })
        }).ToList();
        return Ok(new PagedResult<object>(items, page.Total, page.Page, page.PageSize, page.TotalPages));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Borrowers)]
    public async Task<ActionResult> CreateRequest(
        [FromBody] CreateConsumableRequestDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        dto.Reason = dto.Reason.Trim();
        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            return BadRequest(new { message = "Mục đích cấp phát là bắt buộc." });
        }

        var consumable = await _context.Consumables
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == dto.ConsumableId, cancellationToken);
        if (consumable is null)
        {
            return NotFound(new { message = "Vật tư không tồn tại." });
        }

        var availableQuantity = Math.Max(0, consumable.Quantity - consumable.ReservedQuantity);
        if (dto.Quantity > availableQuantity)
        {
            return BadRequest(new
            {
                message = $"Kho hiện chỉ còn {availableQuantity} {consumable.Unit} chưa được giữ cho yêu cầu khác."
            });
        }

        var hasPendingRequest = await _context.ConsumableRequests.AnyAsync(
            item => item.UserId == userId
                && item.ConsumableId == dto.ConsumableId
                && (item.Status == Pending || item.Status == Approved || item.Status == HandedOver),
            cancellationToken);
        if (hasPendingRequest)
        {
            return Conflict(new { message = "Bạn đã có yêu cầu đang chờ duyệt cho vật tư này." });
        }

        var request = new ConsumableRequest
        {
            UserId = userId,
            ConsumableId = dto.ConsumableId,
            Quantity = dto.Quantity,
            Reason = dto.Reason,
            Status = Pending,
            RequestDate = DateTime.UtcNow
        };
        _context.ConsumableRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Create",
            nameof(ConsumableRequest),
            request.Id,
            new { request.ConsumableId, request.Quantity },
            cancellationToken);

        await _notificationService.NotifyManagersAsync(
            "CONSUMABLE_PENDING",
            "Yêu cầu cấp phát vật tư mới",
            "Có yêu cầu cấp phát vật tư mới.",
            "/dashboard/consumable-requests",
            cancellationToken);
        return Ok(new { request.Id, message = "Đã gửi yêu cầu cấp phát." });
    }

    [HttpPut("{id:int}/approve")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> ApproveRequest(
        int id,
        CancellationToken cancellationToken)
    {
        var request = await _context.ConsumableRequests
            .AsNoTracking()
            .Include(item => item.Consumable)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request is null || request.Status != Pending)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu đang chờ duyệt." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var claimedRequest = await _context.ConsumableRequests
            .Where(item => item.Id == id && item.Status == Pending)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(item => item.Status, Processing),
                cancellationToken);
        if (claimedRequest == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Yêu cầu đã được người khác xử lý." });
        }

        var reservedStock = await _context.Consumables
            .Where(item => item.Id == request.ConsumableId
                && item.Quantity - item.ReservedQuantity >= request.Quantity)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(
                    item => item.ReservedQuantity,
                    item => item.ReservedQuantity + request.Quantity),
                cancellationToken);
        if (reservedStock == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new
            {
                message = "Kho không còn đủ số lượng chưa được giữ cho yêu cầu khác."
            });
        }

        var approvalDate = DateTime.UtcNow;
        await _context.ConsumableRequests
            .Where(item => item.Id == id && item.Status == Processing)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Status, Approved)
                    .SetProperty(item => item.ApprovalDate, (DateTime?)approvalDate),
                cancellationToken);
        await _auditService.WriteAsync(
            HttpContext,
            "Approve",
            nameof(ConsumableRequest),
            id,
            new { request.ConsumableId, request.Quantity },
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _notificationService.NotifyUserAsync(
            request.UserId,
            "CONSUMABLE_APPROVED",
            "Yêu cầu vật tư đã được duyệt",
            "Yêu cầu vật tư đã được duyệt và giữ hàng. Vui lòng chờ quản lý bàn giao.",
            "/dashboard/consumable-requests",
            cancellationToken);
        return Ok(new { message = "Đã duyệt và giữ số lượng vật tư. Bước tiếp theo là bàn giao theo lô." });
    }

    [HttpGet("{id:int}/available-lots")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> GetAvailableLots(int id, CancellationToken cancellationToken)
    {
        var request = await _context.ConsumableRequests.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request is null) return NotFound(new { message = "Không tìm thấy yêu cầu." });
        if (request.Status != Approved)
            return Conflict(new { message = "Chỉ phiếu đã duyệt mới được chọn lô bàn giao." });

        var today = VietnamTime.Today();
        var lots = await _context.ConsumableLots.AsNoTracking()
            .Where(item => item.ConsumableId == request.ConsumableId
                && item.Quantity > 0
                && (!item.ExpiryDate.HasValue || item.ExpiryDate.Value >= VietnamTime.StartOfDayUtc(today)))
            .OrderBy(item => item.ExpiryDate == null)
            .ThenBy(item => item.ExpiryDate)
            .ThenBy(item => item.EntryDate)
            .Select(item => new
            {
                item.Id,
                item.LotNumber,
                item.Quantity,
                item.EntryDate,
                item.ExpiryDate,
                item.StorageLocation
            })
            .ToListAsync(cancellationToken);
        return Ok(new { request.Id, request.Quantity, lots });
    }

    [HttpPut("{id:int}/handover")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> HandoverRequest(
        int id,
        [FromBody] HandoverConsumableDto dto,
        CancellationToken cancellationToken)
    {
        dto.Allocations ??= new();
        var allocations = dto.Allocations
            .Where(item => item.LotId > 0 && item.Quantity > 0)
            .GroupBy(item => item.LotId)
            .Select(group => new LotAllocationDto
            {
                LotId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();
        var request = await _context.ConsumableRequests.AsNoTracking()
            .Include(item => item.Consumable)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request?.Consumable is null || request.Status != Approved)
            return NotFound(new { message = "Không tìm thấy yêu cầu đã duyệt đang chờ bàn giao." });
        if (allocations.Count == 0 || allocations.Sum(item => item.Quantity) != request.Quantity)
            return BadRequest(new { message = "Tổng số lượng chọn từ các lô phải đúng bằng số lượng đã duyệt." });

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var claimed = await _context.ConsumableRequests
            .Where(item => item.Id == id && item.Status == Approved)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(item => item.Status, Processing),
                cancellationToken);
        if (claimed == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Yêu cầu đã được xử lý bởi phiên khác." });
        }

        var lotIds = allocations.Select(item => item.LotId).ToArray();
        var lots = await _context.ConsumableLots.AsNoTracking()
            .Where(item => lotIds.Contains(item.Id) && item.ConsumableId == request.ConsumableId)
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        if (lots.Count != allocations.Count)
        {
            await transaction.RollbackAsync(cancellationToken);
            return BadRequest(new { message = "Có lô không thuộc vật tư được yêu cầu." });
        }

        var today = VietnamTime.Today();
        foreach (var allocation in allocations)
        {
            var lot = lots[allocation.LotId];
            if (lot.ExpiryDate.HasValue && VietnamTime.Date(lot.ExpiryDate.Value) < today)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict(new { message = $"Lô {lot.LotNumber} đã hết hạn." });
            }
            var deducted = await _context.ConsumableLots
                .Where(item => item.Id == allocation.LotId && item.Quantity >= allocation.Quantity)
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(item => item.Quantity, item => item.Quantity - allocation.Quantity),
                    cancellationToken);
            if (deducted == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict(new { message = $"Lô {lot.LotNumber} không còn đủ số lượng." });
            }
        }

        var beforeQuantity = request.Consumable.Quantity;
        var updatedStock = await _context.Consumables
            .Where(item => item.Id == request.ConsumableId
                && item.Quantity >= request.Quantity
                && item.ReservedQuantity >= request.Quantity)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Quantity, item => item.Quantity - request.Quantity)
                    .SetProperty(item => item.ReservedQuantity, item => item.ReservedQuantity - request.Quantity),
                cancellationToken);
        if (updatedStock == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new { message = "Tồn kho hoặc số lượng giữ chỗ đã thay đổi." });
        }

        _context.ConsumableRequestLotAllocations.AddRange(allocations.Select(item =>
            new ConsumableRequestLotAllocation
            {
                ConsumableRequestId = id,
                ConsumableLotId = item.LotId,
                Quantity = item.Quantity
            }));
        _context.ConsumableTransactions.Add(new ConsumableTransaction
        {
            ConsumableId = request.ConsumableId,
            ConsumableRequestId = id,
            Type = "Bàn giao",
            Quantity = request.Quantity,
            BeforeQuantity = beforeQuantity,
            AfterQuantity = beforeQuantity - request.Quantity,
            Reason = $"Bàn giao yêu cầu #{id} theo {allocations.Count} lô",
            UserId = GetCurrentUserId()
        });
        await _context.SaveChangesAsync(cancellationToken);

        var handedOverAt = DateTime.UtcNow;
        await _context.ConsumableRequests
            .Where(item => item.Id == id && item.Status == Processing)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Status, HandedOver)
                    .SetProperty(item => item.HandedOverAt, (DateTime?)handedOverAt)
                    .SetProperty(item => item.HandedOverByUserId, (int?)GetCurrentUserId()),
                cancellationToken);
        await _auditService.WriteAsync(HttpContext, "Handover", nameof(ConsumableRequest), id,
            new { request.ConsumableId, request.Quantity, Allocations = allocations }, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _notificationService.NotifyUserAsync(
            request.UserId,
            "CONSUMABLE_HANDED_OVER",
            "Vật tư đã được bàn giao",
            "Quản lý đã bàn giao vật tư. Vui lòng kiểm tra và xác nhận đã nhận.",
            "/dashboard/consumable-requests",
            cancellationToken);
        return Ok(new { message = "Đã bàn giao theo lô. Đang chờ người nhận xác nhận." });
    }

    [HttpPut("{id:int}/confirm-receipt")]
    [Authorize(Roles = Roles.Borrowers)]
    public async Task<IActionResult> ConfirmReceipt(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var receivedAt = DateTime.UtcNow;
        var updated = await _context.ConsumableRequests
            .Where(item => item.Id == id && item.UserId == userId && item.Status == HandedOver)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Status, Received)
                    .SetProperty(item => item.ReceivedAt, (DateTime?)receivedAt)
                    .SetProperty(item => item.ReceivedByUserId, (int?)userId),
                cancellationToken);
        if (updated == 0)
            return Conflict(new { message = "Yêu cầu không thuộc tài khoản này hoặc chưa được bàn giao." });

        await _auditService.WriteAsync(HttpContext, "ConfirmReceipt", nameof(ConsumableRequest), id,
            cancellationToken: cancellationToken);
        await _notificationService.NotifyManagersAsync(
            "CONSUMABLE_RECEIVED",
            "Người nhận đã xác nhận vật tư",
            $"Yêu cầu cấp phát #{id} đã được người nhận xác nhận.",
            "/dashboard/consumable-requests",
            cancellationToken);
        return Ok(new { message = "Đã xác nhận nhận đủ vật tư." });
    }

    [HttpPut("{id:int}/reject")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> RejectRequest(
        int id,
        CancellationToken cancellationToken)
    {
        var request = await _context.ConsumableRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (request is null)
        {
            return NotFound(new { message = "Không tìm thấy yêu cầu." });
        }

        var approvalDate = DateTime.UtcNow;
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var updated = await _context.ConsumableRequests
            .Where(item => item.Id == id && (item.Status == Pending || item.Status == Approved))
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Status, Rejected)
                    .SetProperty(item => item.ApprovalDate, (DateTime?)approvalDate),
                cancellationToken);
        if (updated == 0)
        {
            return Conflict(new { message = "Yêu cầu đã được xử lý." });
        }

        if (request.Status == Approved)
        {
            var released = await _context.Consumables
                .Where(item => item.Id == request.ConsumableId && item.ReservedQuantity >= request.Quantity)
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(
                        item => item.ReservedQuantity,
                        item => item.ReservedQuantity - request.Quantity),
                    cancellationToken);
            if (released == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Conflict(new { message = "Không thể hoàn lại số lượng giữ chỗ." });
            }
        }

        await _auditService.WriteAsync(
            HttpContext,
            "Reject",
            nameof(ConsumableRequest),
            id,
            cancellationToken: cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _notificationService.NotifyUserAsync(
            request.UserId,
            "CONSUMABLE_REJECTED",
            "Yêu cầu vật tư bị từ chối",
            "Yêu cầu vật tư của bạn đã bị từ chối.",
            "/dashboard/consumable-requests",
            cancellationToken);
        return Ok(new { message = "Đã từ chối yêu cầu." });
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
