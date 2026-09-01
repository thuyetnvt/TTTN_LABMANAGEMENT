using System.Security.Claims;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
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
        var now = DateTime.UtcNow;
        var today = VietnamTime.Today(now);
        var todayStartUtc = VietnamTime.StartOfDayUtc(today);

        var equipmentCounts = await _context.Equipments
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Available = group.Count(item => item.Status == EquipmentStatuses.Available),
                BorrowPending = group.Count(item => item.Status == EquipmentStatuses.BorrowPending),
                Borrowed = group.Count(item => item.Status == EquipmentStatuses.Borrowed),
                Broken = group.Count(item => item.Status == EquipmentStatuses.Broken),
                Missing = group.Count(item => item.Status == EquipmentStatuses.Missing),
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
                    ? $"{userName} đã yêu cầu mượn {equipmentName} ({StatusCodeMap.Label(record.Status)})"
                    : $"Bạn đã yêu cầu mượn {equipmentName} ({StatusCodeMap.Label(record.Status)})",
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
                    $"{record.Equipment!.Name} được bảo trì ({StatusCodeMap.Label(record.Status)})",
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
                && record.ExpectedReturnDate < todayStartUtc);
        if (!isManager)
        {
            overdueQuery = overdueQuery.Where(record => record.UserId == userId);
        }

        var overdueRecords = await overdueQuery.ToListAsync(cancellationToken);
        var alerts = overdueRecords.Select(record =>
        {
            var days = Math.Max(1, (today - VietnamTime.Date(record.ExpectedReturnDate)).Days);
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
        var pendingBorrowRequests = 0;
        var pendingConsumableRequests = 0;
        var borrowRequestsToProcess = 0;
        var consumableRequestsToProcess = 0;
        var overdueBorrowRecords = overdueRecords.Count;
        var lowStockConsumableCount = 0;
        var warrantyExpiringSoon = 0;
        var lowStockConsumables = new List<object>();
        var borrowTrends = new List<object>();

        if (isManager)
        {
            totalUsers = await _context.Users.CountAsync(
                user => user.IsActive,
                cancellationToken);
            totalPenalties = await _context.Penalties
                .Where(penalty => penalty.Status == PenaltyStatuses.Unpaid)
                .SumAsync(penalty => penalty.Amount, cancellationToken);
            pendingBorrowRequests = await _context.BorrowRecords
                .CountAsync(record => record.Status == BorrowStatuses.Pending, cancellationToken);
            pendingConsumableRequests = await _context.ConsumableRequests
                .CountAsync(request => request.Status == ConsumableRequestStatuses.Pending, cancellationToken);
            borrowRequestsToProcess = await _context.BorrowRecords.CountAsync(
                record => record.Status == BorrowStatuses.Pending
                    || record.Status == BorrowStatuses.Approved
                    || record.Status == BorrowStatuses.ReturnProcessing,
                cancellationToken);
            consumableRequestsToProcess = await _context.ConsumableRequests.CountAsync(
                request => request.Status == ConsumableRequestStatuses.Pending
                    || request.Status == ConsumableRequestStatuses.Approved,
                cancellationToken);
            pendingRequests = borrowRequestsToProcess + consumableRequestsToProcess;

            var lowStockItems = await _context.Consumables
                .AsNoTracking()
                .Where(consumable => consumable.Quantity - consumable.ReservedQuantity <= consumable.MinQuantity)
                .OrderBy(consumable => consumable.Quantity - consumable.ReservedQuantity - consumable.MinQuantity)
                .Select(consumable => new
                {
                    consumable.Name,
                    consumable.Quantity,
                    consumable.ReservedQuantity,
                    AvailableQuantity = consumable.Quantity - consumable.ReservedQuantity,
                    consumable.MinQuantity
                })
                .ToListAsync(cancellationToken);
            lowStockConsumables = lowStockItems.Cast<object>().ToList();
            lowStockConsumableCount = lowStockItems.Count;

            foreach (var item in lowStockItems.Take(5))
            {
                alerts.Add(new
                {
                    Type = "low-stock",
                    Title = "Vật tư sắp hết",
                    Message = $"{item.Name} còn khả dụng {item.AvailableQuantity}/{item.MinQuantity}.",
                    Level = "warning"
                });
            }

            if (pendingBorrowRequests > 0)
            {
                alerts.Add(new
                {
                    Type = "pending-borrow-requests",
                    Title = "Yêu cầu mượn chờ duyệt",
                    Message = $"Có {pendingBorrowRequests} yêu cầu mượn cần xử lý.",
                    Level = "info"
                });
            }

            if (pendingConsumableRequests > 0)
            {
                alerts.Add(new
                {
                    Type = "pending-consumable-requests",
                    Title = "Yêu cầu cấp phát chờ duyệt",
                    Message = $"Có {pendingConsumableRequests} yêu cầu cấp phát cần xử lý.",
                    Level = "info"
                });
            }

            var warrantySoonQuery = _context.Equipments
                .AsNoTracking()
                .Where(equipment => equipment.WarrantyExpiry.HasValue
                    && equipment.WarrantyExpiry.Value >= now
                    && equipment.WarrantyExpiry.Value <= now.AddDays(30));
            warrantyExpiringSoon = await warrantySoonQuery.CountAsync(cancellationToken);
            var warrantySoon = await warrantySoonQuery
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
                now.Year,
                now.Month,
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
            UpdatedAt = now,
            Counts = new
            {
                Total = equipmentCounts?.Total ?? 0,
                Available = equipmentCounts?.Available ?? 0,
                BorrowPending = equipmentCounts?.BorrowPending ?? 0,
                Maintenance = equipmentCounts?.Maintenance ?? 0,
                Borrowed = equipmentCounts?.Borrowed ?? 0,
                Broken = equipmentCounts?.Broken ?? 0,
                Missing = equipmentCounts?.Missing ?? 0,
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
            },
            PendingBorrowRequests = pendingBorrowRequests,
            PendingConsumableRequests = pendingConsumableRequests,
            BorrowRequestsToProcess = borrowRequestsToProcess,
            ConsumableRequestsToProcess = consumableRequestsToProcess,
            OverdueBorrowRecords = overdueBorrowRecords,
            LowStockConsumables = lowStockConsumableCount,
            WarrantyExpiringSoon = warrantyExpiringSoon,
            MaintenanceInProgress = equipmentCounts?.Maintenance ?? 0
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
