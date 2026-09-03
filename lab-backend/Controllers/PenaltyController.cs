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
public class PenaltyController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public PenaltyController(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetPenalties(
        CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var query = _context.Penalties
            .AsNoTracking()
            .Include(penalty => penalty.User)
            .Include(penalty => penalty.Equipment)
            .AsQueryable();

        if (role is Roles.Student or Roles.Teacher)
        {
            query = query.Where(penalty => penalty.UserId == userId);
        }

        var penalties = await query
            .OrderByDescending(penalty => penalty.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(penalties.Select(penalty => new
        {
            penalty.Id,
            penalty.UserId,
            Username = penalty.User?.Username,
            FullName = penalty.User?.FullName,
            penalty.EquipmentId,
            EquipmentName = penalty.Equipment?.Name,
            penalty.BorrowRecordId,
            Reason = SeedDisplayText.Clean(penalty.Reason),
            penalty.Amount,
            penalty.Status,
            penalty.CreatedAt,
            penalty.PaidAt
        }));
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPenaltiesPaged(
        [FromQuery] PageQuery paging,
        CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var query = _context.Penalties
            .AsNoTracking()
            .Include(penalty => penalty.User)
            .Include(penalty => penalty.Equipment)
            .AsQueryable();
        if (role is Roles.Student or Roles.Teacher)
        {
            query = query.Where(penalty => penalty.UserId == userId);
        }
        var search = paging.NormalizedSearch;
        if (search.Length > 0)
        {
            query = query.Where(penalty =>
                penalty.User!.Username.Contains(search)
                || penalty.User.FullName.Contains(search)
                || penalty.Equipment!.Name.Contains(search)
                || penalty.Reason.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(paging.Status))
        {
            var status = paging.Status.Trim();
            query = query.Where(penalty => penalty.Status == status);
        }
        if (paging.From.HasValue) query = query.Where(penalty => penalty.CreatedAt >= paging.From.Value);
        if (paging.To.HasValue)
        {
            var exclusiveTo = paging.To.Value.Date.AddDays(1);
            query = query.Where(penalty => penalty.CreatedAt < exclusiveTo);
        }

        var page = await query
            .AsSingleQuery()
            .OrderByDescending(penalty => penalty.CreatedAt)
            .ThenByDescending(penalty => penalty.Id)
            .ToPagedResultAsync(paging, cancellationToken);
        var items = page.Items.Select(penalty => (object)new
        {
            penalty.Id,
            penalty.UserId,
            Username = penalty.User?.Username,
            FullName = penalty.User?.FullName,
            penalty.EquipmentId,
            EquipmentName = penalty.Equipment?.Name,
            penalty.BorrowRecordId,
            Reason = SeedDisplayText.Clean(penalty.Reason),
            penalty.Amount,
            penalty.Status,
            penalty.CreatedAt,
            penalty.PaidAt
        }).ToList();
        return Ok(new PagedResult<object>(items, page.Total, page.Page, page.PageSize, page.TotalPages));
    }

    [HttpPut("{id:int}/pay")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> PayPenalty(
        int id,
        CancellationToken cancellationToken)
    {
        var paidAt = DateTime.UtcNow;
        var updated = await _context.Penalties
            .Where(penalty => penalty.Id == id && penalty.Status == PenaltyStatuses.Unpaid)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(penalty => penalty.Status, PenaltyStatuses.Paid)
                    .SetProperty(penalty => penalty.PaidAt, (DateTime?)paidAt),
                cancellationToken);
        if (updated == 0)
        {
            var exists = await _context.Penalties.AnyAsync(
                penalty => penalty.Id == id,
                cancellationToken);
            return exists
                ? Conflict(new { message = "Biên bản này đã được thanh toán." })
                : NotFound();
        }

        await _auditService.WriteAsync(
            HttpContext,
            "MarkPaid",
            nameof(Penalty),
            id,
            new { PaidAt = paidAt },
            cancellationToken);
        return Ok(new { message = "Đã xác nhận thanh toán." });
    }
}
