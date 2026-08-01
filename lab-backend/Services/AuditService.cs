using System.Security.Claims;
using System.Text.Json;
using LabManagementAPI.Data;
using LabManagementAPI.Models;

namespace LabManagementAPI.Services;

public interface IAuditService
{
    Task WriteAsync(
        HttpContext httpContext,
        string action,
        string entityType,
        object? entityId = null,
        object? details = null,
        CancellationToken cancellationToken = default);
}

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task WriteAsync(
        HttpContext httpContext,
        string action,
        string entityType,
        object? entityId = null,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = int.TryParse(userIdValue, out var userId) ? userId : null,
            Username = httpContext.User.Identity?.Name ?? string.Empty,
            Action = action,
            EntityType = entityType,
            EntityId = entityId?.ToString() ?? string.Empty,
            Details = details == null ? string.Empty : JsonSerializer.Serialize(details),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}
