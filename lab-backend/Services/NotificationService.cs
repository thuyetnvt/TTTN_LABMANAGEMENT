using LabManagementAPI.Data;
using LabManagementAPI.Hubs;
using LabManagementAPI.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Services;

public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public Task NotifyUserAsync(int userId, string type, string title, string message, string url, CancellationToken cancellationToken)
        => NotifyUsersAsync([userId], type, title, message, url, cancellationToken);

    public async Task NotifyUsersAsync(
        IEnumerable<int> userIds,
        string type,
        string title,
        string message,
        string url,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0) return;

        var notifications = ids.Select(userId => new AppNotification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Url = url
        }).ToList();
        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync(cancellationToken);
        foreach (var userId in ids)
        {
            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("ReceiveNotification", new { title, message, url }, cancellationToken);
        }
    }

    public async Task NotifyManagersAsync(
        string type,
        string title,
        string message,
        string url,
        CancellationToken cancellationToken)
    {
        var managerIds = await _context.Users
            .AsNoTracking()
            .Where(user => user.IsActive && (user.Role == Roles.Admin || user.Role == Roles.LabHead || user.Role == Roles.DeputyLabHead))
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);
        await NotifyUsersAsync(managerIds, type, title, message, url, cancellationToken);
    }
}
