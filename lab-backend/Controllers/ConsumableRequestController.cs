using System.ComponentModel.DataAnnotations;
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
public class ConsumableRequestController : ControllerBase
{
    private const string Pending = ConsumableRequestStatuses.Pending;
    private const string Processing = ConsumableRequestStatuses.Processing;
    private const string Issued = ConsumableRequestStatuses.Issued;
    private const string Rejected = ConsumableRequestStatuses.Rejected;

    private readonly AppDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IAuditService _auditService;

    public ConsumableRequestController(
        AppDbContext context,
        IHubContext<NotificationHub> hubContext,
        IAuditService auditService)
    {
        _context = context;
        _hubContext = hubContext;
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
            request.ApprovalDate
        }));
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

        if (dto.Quantity > consumable.Quantity)
        {
            return BadRequest(new
            {
                message = $"Kho hiện chỉ còn {consumable.Quantity} {consumable.Unit}."
            });
        }

        var hasPendingRequest = await _context.ConsumableRequests.AnyAsync(
            item => item.UserId == userId
                && item.ConsumableId == dto.ConsumableId
                && item.Status == Pending,
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

        await _hubContext.Clients.Group(NotificationHub.ManagerGroup)
            .SendAsync("ReceiveNotification", "Có yêu cầu cấp phát vật tư mới.", cancellationToken);
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

        var reducedStock = await _context.Consumables
            .Where(item => item.Id == request.ConsumableId
                && item.Quantity >= request.Quantity)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(
                    item => item.Quantity,
                    item => item.Quantity - request.Quantity),
                cancellationToken);
        if (reducedStock == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Conflict(new
            {
                message = $"Kho không đủ vật tư. Hiện chỉ còn {request.Consumable?.Quantity ?? 0} {request.Consumable?.Unit}."
            });
        }

        var approvalDate = DateTime.UtcNow;
        var afterQuantity = await _context.Consumables
            .Where(item => item.Id == request.ConsumableId)
            .Select(item => item.Quantity)
            .SingleAsync(cancellationToken);

        _context.ConsumableTransactions.Add(new ConsumableTransaction
        {
            ConsumableId = request.ConsumableId,
            Type = "Cấp phát",
            Quantity = request.Quantity,
            BeforeQuantity = afterQuantity + request.Quantity,
            AfterQuantity = afterQuantity,
            Reason = $"Duyệt yêu cầu cấp phát #{id}",
            UserId = GetCurrentUserId(),
            CreatedAt = approvalDate
        });

        await _context.ConsumableRequests
            .Where(item => item.Id == id && item.Status == Processing)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Status, Issued)
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

        await _hubContext.Clients.User(request.UserId.ToString())
            .SendAsync("ReceiveNotification", "Yêu cầu vật tư của bạn đã được cấp phát.", cancellationToken);
        return Ok(new { message = "Đã duyệt và trừ tồn kho." });
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
        var updated = await _context.ConsumableRequests
            .Where(item => item.Id == id && item.Status == Pending)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Status, Rejected)
                    .SetProperty(item => item.ApprovalDate, (DateTime?)approvalDate),
                cancellationToken);
        if (updated == 0)
        {
            return Conflict(new { message = "Yêu cầu đã được xử lý." });
        }

        await _auditService.WriteAsync(
            HttpContext,
            "Reject",
            nameof(ConsumableRequest),
            id,
            cancellationToken: cancellationToken);

        await _hubContext.Clients.User(request.UserId.ToString())
            .SendAsync("ReceiveNotification", "Yêu cầu vật tư của bạn đã bị từ chối.", cancellationToken);
        return Ok(new { message = "Đã từ chối yêu cầu." });
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
