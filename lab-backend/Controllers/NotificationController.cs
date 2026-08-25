using System.Security.Claims;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<object>> Get(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] bool? unreadOnly,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var query = _context.Notifications
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Where(item => unreadOnly != true || !item.IsRead)
            .OrderByDescending(item => item.CreatedAt);

        // Keep the legacy array response when no pagination/filter query is supplied.
        if (!page.HasValue && !pageSize.HasValue && !unreadOnly.HasValue)
        {
            var legacyItems = await query
                .Take(50)
                .Select(SelectNotification())
                .ToListAsync(cancellationToken);
            return Ok(legacyItems);
        }

        var safePage = Math.Max(1, page ?? 1);
        var safePageSize = Math.Clamp(pageSize ?? 20, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(item => new
            {
                item.Id,
                item.Type,
                item.Title,
                item.Message,
                item.Url,
                item.IsRead,
                item.CreatedAt,
                item.ReadAt
            })
            .ToListAsync(cancellationToken);
        return Ok(new
        {
            Items = items,
            Page = safePage,
            PageSize = safePageSize,
            Total = total,
            HasNextPage = safePage * safePageSize < total
        });
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var count = await _context.Notifications.CountAsync(item => item.UserId == userId && !item.IsRead, cancellationToken);
        return Ok(new { count });
    }

    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken cancellationToken)
    {
        var item = await _context.Notifications
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == GetCurrentUserId(), cancellationToken);
        if (item is not null)
        {
            if (!item.IsRead)
            {
                item.IsRead = true;
                item.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
            return NoContent();
        }

        return NotFound(new { message = "Không tìm thấy thông báo." });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var items = await _context.Notifications
            .Where(item => item.UserId == GetCurrentUserId() && !item.IsRead)
            .ToListAsync(cancellationToken);
        var readAt = DateTime.UtcNow;
        foreach (var item in items)
        {
            item.IsRead = true;
            item.ReadAt = readAt;
        }
        if (items.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        return NoContent();
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static System.Linq.Expressions.Expression<Func<AppNotification, object>> SelectNotification()
        => item => new
        {
            item.Id,
            item.Type,
            item.Title,
            item.Message,
            item.Url,
            item.IsRead,
            item.CreatedAt,
            item.ReadAt
        };
}
