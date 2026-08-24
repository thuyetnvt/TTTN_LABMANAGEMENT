using System.Security.Claims;
using LabManagementAPI.Data;
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
    public async Task<ActionResult<IEnumerable<object>>> Get(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var items = await _context.Notifications
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(50)
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
        return Ok(items);
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
        var updated = await _context.Notifications
            .Where(item => item.Id == id && item.UserId == GetCurrentUserId() && !item.IsRead)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(item => item.IsRead, true)
                .SetProperty(item => item.ReadAt, DateTime.UtcNow), cancellationToken);
        return updated == 0 ? NotFound(new { message = "Không tìm thấy thông báo chưa đọc." }) : NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _context.Notifications
            .Where(item => item.UserId == GetCurrentUserId() && !item.IsRead)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(item => item.IsRead, true)
                .SetProperty(item => item.ReadAt, DateTime.UtcNow), cancellationToken);
        return NoContent();
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
