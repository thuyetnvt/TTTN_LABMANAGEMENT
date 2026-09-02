using System.Security.Claims;
using System.Text.Json;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

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
        var resolvedUserId = int.TryParse(userIdValue, out var userId) ? userId : (int?)null;
        var resolvedUsername = httpContext.User.Identity?.Name?.Trim() ?? string.Empty;
        var entityIdValue = entityId?.ToString() ?? string.Empty;
        var serializedDetails = details == null ? string.Empty : JsonSerializer.Serialize(details);

        // Các thao tác xác thực được ghi trước khi JWT tồn tại nên HttpContext.User chưa có danh tính.
        // Với log gắn trực tiếp tới người dùng, lấy lại tên từ tài khoản để cột người thao tác không bị trống.
        if ((!resolvedUserId.HasValue || string.IsNullOrWhiteSpace(resolvedUsername))
            && entityType == nameof(User)
            && int.TryParse(entityIdValue, out var entityUserId))
        {
            var actor = await _context.Users
                .AsNoTracking()
                .Where(item => item.Id == entityUserId)
                .Select(item => new { item.Id, item.Username })
                .SingleOrDefaultAsync(cancellationToken);
            if (actor != null)
            {
                resolvedUserId ??= actor.Id;
                resolvedUsername = actor.Username;
            }
        }

        // Đăng nhập thất bại không có UserId; vẫn lưu tài khoản đã nhập để quản trị viên truy vết.
        if (string.IsNullOrWhiteSpace(resolvedUsername))
        {
            resolvedUsername = ExtractUsername(serializedDetails) ?? string.Empty;
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = resolvedUserId,
            Username = resolvedUsername,
            Action = action,
            EntityType = entityType,
            EntityId = entityIdValue,
            Details = serializedDetails,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? ExtractUsername(string details)
    {
        if (string.IsNullOrWhiteSpace(details)) return null;

        try
        {
            using var document = JsonDocument.Parse(details);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Name.Equals("username", StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString()?.Trim();
                }
            }
        }
        catch (JsonException)
        {
            // Chi tiết cũ có thể không phải JSON; giữ log thay vì làm hỏng thao tác chính.
        }

        return null;
    }
}
