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
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isManager = role is Roles.Admin or Roles.LabHead or Roles.DeputyLabHead;

        var equipmentCounts = await _context.Equipments
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Available = group.Count(item => item.Status == EquipmentStatuses.Available),
                Borrowed = group.Count(item => item.Status == EquipmentStatuses.Borrowed),
                Broken = group.Count(item => item.Status == EquipmentStatuses.Broken),
                Warranty = group.Count(item => item.Status == EquipmentStatuses.Warranty),
                Maintenance = group.Count(item => item.Status == EquipmentStatuses.MaintenanceInProgress)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var recentBorrowQuery = _context.BorrowRecords
            .AsNoTracking()
            .Include(record => record.User)
            .Include(record => record.Equipment)
            .Include(record => record.Details)
                .ThenInclude(detail => detail.Equipment)
            .AsQueryable();
        if (!isManager)
        {
            recentBorrowQuery = recentBorrowQuery.Where(record => record.UserId == userId);
        }

        var recentBorrows = await recentBorrowQuery
            .OrderByDescending(record => record.BorrowDate)
            .Take(5)
            .ToListAsync(cancellationToken);
        var borrowActivities = recentBorrows.Select(record =>
        {
            var equipmentName = EquipmentLabel(record);
            var userName = record.User?.Username ?? "Người dùng";
            return new DashboardActivity(
                "borrow",
                isManager
                    ? $"{userName} đã yêu cầu mượn {equipmentName} ({record.Status})"
                    : $"Bạn đã yêu cầu mượn {equipmentName} ({record.Status})",
                record.BorrowDate,
                record.Status == BorrowStatuses.Returned
                    ? "blue"
                    : record.Status is BorrowStatuses.Pending or BorrowStatuses.TeacherPending
                        ? "orange"
                        : "green");
        });

        var activities = new List<DashboardActivity>(borrowActivities);
        if (isManager)
        {
            var maintenanceActivities = await _context.MaintenanceRecords
                .AsNoTracking()
                .Include(record => record.Equipment)
                .OrderByDescending(record => record.MaintenanceDate)
                .Take(5)
                .Select(record => new DashboardActivity(
                    "maintenance",
                    $"{record.Equipment!.Name} được bảo trì ({record.Status})",
                    record.MaintenanceDate,
                    "red"))
                .ToListAsync(cancellationToken);
            activities.AddRange(maintenanceActivities);
        }

        var recentActivities = activities
            .OrderByDescending(activity => activity.Date)
            .Take(5)
            .ToList();

        var overdueQuery = _context.BorrowRecords
            .AsNoTracking()
            .Include(record => record.User)
            .Include(record => record.Equipment)
            .Include(record => record.Details)
                .ThenInclude(detail => detail.Equipment)
            .Where(record => record.Status == BorrowStatuses.Borrowed
                && record.ExpectedReturnDate < DateTime.UtcNow);
        if (!isManager)
        {
            overdueQuery = overdueQuery.Where(record => record.UserId == userId);
        }

        var overdueRecords = await overdueQuery.ToListAsync(cancellationToken);
        var alerts = overdueRecords.Select(record =>
        {
            var days = Math.Max(1, (DateTime.UtcNow.Date - record.ExpectedReturnDate.Date).Days);
            var personName = isManager ? record.User?.Username ?? "Người dùng" : "Bạn";
            return new
            {
                Type = "overdue",
                Title = "Quá hạn mượn thiết bị",
                Message = $"{personName} đang mượn {EquipmentLabel(record)} quá hạn {days} ngày.",
                Level = "error"
            } as object;
        }).ToList();

        var totalUsers = 0;
        decimal totalPenalties = 0;
        var pendingRequests = 0;
        var lowStockConsumables = new List<object>();
        var borrowTrends = new List<object>();

        if (isManager)
        {
            totalUsers = await _context.Users.CountAsync(
                user => user.IsActive,
                cancellationToken);
            totalPenalties = await _context.Penalties
                .Where(penalty => penalty.Status == PenaltyStatuses.Paid)
                .SumAsync(penalty => penalty.Amount, cancellationToken);
            var pendingBorrows = await _context.BorrowRecords
                .CountAsync(record =>
                    record.Status == BorrowStatuses.Pending
                    || record.Status == BorrowStatuses.TeacherPending,
                    cancellationToken);
            var pendingConsumables = await _context.ConsumableRequests
                .CountAsync(request => request.Status == ConsumableRequestStatuses.Pending, cancellationToken);
            pendingRequests = pendingBorrows + pendingConsumables;

            var lowStockItems = await _context.Consumables
                .AsNoTracking()
                .Where(consumable => consumable.Quantity <= consumable.MinQuantity)
                .OrderBy(consumable => consumable.Quantity - consumable.MinQuantity)
                .Select(consumable => new
                {
                    consumable.Name,
                    consumable.Quantity,
                    consumable.MinQuantity
                })
                .ToListAsync(cancellationToken);
            lowStockConsumables = lowStockItems.Cast<object>().ToList();

            foreach (var item in lowStockItems.Take(5))
            {
                alerts.Add(new
                {
                    Type = "low-stock",
                    Title = "Vật tư sắp hết",
                    Message = $"{item.Name} chỉ còn {item.Quantity}/{item.MinQuantity}.",
                    Level = "warning"
                });
            }

            if (pendingBorrows > 0)
            {
                alerts.Add(new
                {
                    Type = "pending-borrow-requests",
                    Title = "Yêu cầu mượn chờ duyệt",
                    Message = $"Có {pendingBorrows} yêu cầu mượn cần xử lý.",
                    Level = "info"
                });
            }

            if (pendingConsumables > 0)
            {
                alerts.Add(new
                {
                    Type = "pending-consumable-requests",
                    Title = "Yêu cầu cấp phát chờ duyệt",
                    Message = $"Có {pendingConsumables} yêu cầu cấp phát cần xử lý.",
                    Level = "info"
                });
            }

            var warrantySoon = await _context.Equipments
                .AsNoTracking()
                .Where(equipment => equipment.WarrantyExpiry.HasValue
                    && equipment.WarrantyExpiry.Value >= DateTime.UtcNow
                    && equipment.WarrantyExpiry.Value <= DateTime.UtcNow.AddDays(30))
                .OrderBy(equipment => equipment.WarrantyExpiry)
                .Take(5)
                .Select(equipment => new
                {
                    equipment.Name,
                    equipment.WarrantyExpiry
                })
                .ToListAsync(cancellationToken);

            foreach (var equipment in warrantySoon)
            {
                alerts.Add(new
                {
                    Type = "warranty-soon",
                    Title = "Thiết bị sắp hết bảo hành",
                    Message = $"{equipment.Name} hết bảo hành ngày {equipment.WarrantyExpiry:dd/MM/yyyy}.",
                    Level = "warning"
                });
            }

            var firstMonth = new DateTime(
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc).AddMonths(-5);
            var groupedBorrows = await _context.BorrowRecords
                .AsNoTracking()
                .Where(record => record.BorrowDate >= firstMonth)
                .GroupBy(record => new
                {
                    record.BorrowDate.Year,
                    record.BorrowDate.Month
                })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Count = group.Count()
                })
                .ToListAsync(cancellationToken);

            var countsByMonth = groupedBorrows.ToDictionary(
                item => item.Year * 100 + item.Month,
                item => item.Count);
            borrowTrends = Enumerable.Range(0, 6)
                .Select(offset => firstMonth.AddMonths(offset))
                .Select(month => new
                {
                    Month = $"{month.Month}/{month.Year}",
                    Count = countsByMonth.GetValueOrDefault(month.Year * 100 + month.Month)
                } as object)
                .ToList();
        }

        return Ok(new
        {
            Counts = new
            {
                Total = equipmentCounts?.Total ?? 0,
                Available = equipmentCounts?.Available ?? 0,
                Maintenance = equipmentCounts?.Maintenance ?? 0,
                Borrowed = equipmentCounts?.Borrowed ?? 0,
                Broken = equipmentCounts?.Broken ?? 0,
                Warranty = equipmentCounts?.Warranty ?? 0
            },
            Activities = recentActivities,
            Alerts = alerts,
            Advanced = new
            {
                TotalUsers = totalUsers,
                TotalPenalties = totalPenalties,
                PendingRequests = pendingRequests,
                LowStockConsumables = lowStockConsumables,
                BorrowTrends = borrowTrends
            }
        });
    }

    private sealed record DashboardActivity(
        string Type,
        string Message,
        DateTime Date,
        string Color);

    private static string EquipmentLabel(BorrowRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.Equipment?.Name))
        {
            return record.Equipment.Name;
        }

        var detailNames = record.Details
            .Select(detail => detail.Equipment?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (detailNames.Count == 1)
        {
            return detailNames[0]!;
        }

        if (record.Details.Count > 0)
        {
            return $"Nhiều tài sản ({record.Details.Count})";
        }

        return "thiết bị chưa xác định";
    }
}
