using LabManagementAPI.Data;
using LabManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LabManagementAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = Roles.Admin)]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuditController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(log => log.Action == action.Trim());
        }
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(log => log.EntityType == entityType.Trim());
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            var matchingActors = await _context.Users
                    .AsNoTracking()
                    .Where(user => user.Username.Contains(keyword) || user.FullName.Contains(keyword))
                    .Select(user => new { user.Id, user.Username })
                    .ToListAsync(cancellationToken);
            var matchingUserIds = matchingActors.Select(user => user.Id.ToString()).ToList();
            var matchingActorIds = matchingActors.Select(user => user.Id).ToList();
            var matchingActorUsernames = matchingActors.Select(user => user.Username).ToList();
            query = query.Where(log =>
                log.Username.Contains(keyword)
                || matchingActorUsernames.Contains(log.Username)
                || (log.UserId.HasValue && matchingActorIds.Contains(log.UserId.Value))
                || log.Action.Contains(keyword)
                || log.EntityType.Contains(keyword)
                || log.Details.Contains(keyword)
                || matchingUserIds.Contains(log.EntityId));
        }
        if (from.HasValue)
        {
            query = query.Where(log => log.CreatedAt >= from.Value);
        }
        if (to.HasValue)
        {
            var exclusiveTo = to.Value.Date.AddDays(1);
            query = query.Where(log => log.CreatedAt < exclusiveTo);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(log => log.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Khôi phục tài khoản người thao tác cho các log cũ được tạo khi đăng nhập chưa có JWT.
        var missingActorIds = items
            .Where(log => string.IsNullOrWhiteSpace(log.Username))
            .Select(ResolveActorUserId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var actorUsersById = missingActorIds.Count == 0
            ? new Dictionary<int, User>()
            : await _context.Users
                .AsNoTracking()
                .Where(user => missingActorIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, cancellationToken);

        foreach (var log in items.Where(item => string.IsNullOrWhiteSpace(item.Username)))
        {
            var actorId = ResolveActorUserId(log);
            if (actorId.HasValue && actorUsersById.TryGetValue(actorId.Value, out var actor))
            {
                log.Username = actor.Username;
                continue;
            }

            log.Username = ExtractUsername(log.Details) ?? "Không xác định";
        }

        // Tên hiển thị lấy theo UserId để vẫn đúng khi tài khoản đã đổi tên đăng nhập.
        var actorIds = items
            .Where(log => log.UserId.HasValue)
            .Select(log => log.UserId!.Value)
            .Concat(items.Select(ResolveActorUserId).Where(id => id.HasValue).Select(id => id!.Value))
            .Distinct()
            .ToList();
        var usersById = actorIds.Count == 0
            ? new Dictionary<int, User>()
            : await _context.Users
                .AsNoTracking()
                .Where(user => actorIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, cancellationToken);
        var usernames = items
            .Select(log => log.Username.Trim())
            .Where(username => !string.IsNullOrWhiteSpace(username))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var usersByUsername = usernames.Count == 0
            ? new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
            : (await _context.Users
                .AsNoTracking()
                .Where(user => usernames.Contains(user.Username))
                .ToListAsync(cancellationToken))
                .ToDictionary(user => user.Username, StringComparer.OrdinalIgnoreCase);

        var responseItems = items.Select(log => new
        {
            log.Id,
            log.UserId,
            log.Username,
            actorDisplayName = FormatActorDisplayName(log, usersById, usersByUsername),
            log.Action,
            log.EntityType,
            log.EntityId,
            log.Details,
            log.IpAddress,
            log.CreatedAt
        });

        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new { page, pageSize, total, totalPages, items = responseItems });
    }

    private static string FormatActorDisplayName(
        AuditLog log,
        IReadOnlyDictionary<int, User> usersById,
        IReadOnlyDictionary<string, User> usersByUsername)
    {
        var username = log.Username.Trim();
        if (string.IsNullOrWhiteSpace(username)) return "Không xác định";
        if (username.Equals("system", StringComparison.OrdinalIgnoreCase)
            || username.Equals("Hệ thống", StringComparison.OrdinalIgnoreCase))
        {
            return "Hệ thống";
        }

        User? user = null;
        if (log.UserId.HasValue)
        {
            usersById.TryGetValue(log.UserId.Value, out user);
        }
        user ??= usersByUsername.TryGetValue(username, out var matchedUser) ? matchedUser : null;

        var fullName = user?.FullName?.Trim();
        return string.IsNullOrWhiteSpace(fullName) || fullName.Equals(username, StringComparison.OrdinalIgnoreCase)
            ? username
            : $"{fullName} ({username})";
    }

    private static int? ResolveActorUserId(AuditLog log)
    {
        if (log.UserId.HasValue) return log.UserId.Value;
        return log.EntityType == nameof(User) && int.TryParse(log.EntityId, out var userId)
            ? userId
            : null;
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
            // Log cũ có thể chứa chuỗi tự do thay vì JSON.
        }

        return null;
    }
}
