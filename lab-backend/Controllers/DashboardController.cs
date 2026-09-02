using System.Security.Claims;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LabManagementAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan DashboardCacheDuration = TimeSpan.FromSeconds(20);

    public DashboardController(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isManager = role is Roles.Admin or Roles.LabHead or Roles.DeputyLabHead;
        var cacheKey = isManager
            ? "dashboard:v4:manager"
            : $"dashboard:v4:{role}:{userId}";
        var forceRefresh = bool.TryParse(Request.Query["refresh"], out var refreshRequested)
            && refreshRequested;
        if (!forceRefresh
            && _cache.TryGetValue(cacheKey, out object? cachedPayload)
            && cachedPayload != null)
        {
            return Ok(cachedPayload);
        }

        var now = DateTime.UtcNow;
        var today = VietnamTime.Today(now);
        var todayStartUtc = VietnamTime.StartOfDayUtc(today);

        var equipmentCounts = !isManager
            ? null
            : await _context.Equipments
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
            .AsSingleQuery()
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
                record.Status is BorrowStatuses.Rejected or BorrowStatuses.ReturnedDamaged
                    ? "red"
                    : record.Status is BorrowStatuses.Pending or BorrowStatuses.TeacherPending or BorrowStatuses.ProcessingApproval
                        ? "orange"
                        : record.Status == BorrowStatuses.Returned
                            ? "green"
                            : "blue");
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
                    record.Status == MaintenanceStatuses.Completed
                        ? "green"
                        : record.Status == MaintenanceStatuses.Completing ? "purple" : "blue"))
                .ToListAsync(cancellationToken);
            activities.AddRange(maintenanceActivities);
        }
        else if (role == Roles.Teacher)
        {
            var sponsoredRequests = await _context.BorrowRecords
                .AsNoTracking()
                .Include(record => record.User)
                .Include(record => record.Equipment)
                .Include(record => record.Details)
                    .ThenInclude(detail => detail.Equipment)
                .Where(record => record.TeacherId == userId)
                .AsSingleQuery()
                .OrderByDescending(record => record.BorrowDate)
                .Take(5)
                .ToListAsync(cancellationToken);
            activities.AddRange(sponsoredRequests.Select(record => new DashboardActivity(
                "teacher-approval",
                $"{record.User?.Username ?? "Sinh viên"} nhờ bạn bảo lãnh {EquipmentLabel(record)} ({StatusCodeMap.Label(record.Status)})",
                record.BorrowDate,
                record.Status == BorrowStatuses.TeacherPending
                    ? "orange"
                    : record.Status == BorrowStatuses.Rejected ? "red" : "green")));
        }

        var recentActivities = activities
            .OrderByDescending(activity => activity.Date)
            .Take(5)
            .ToList();

        var overdueBaseQuery = _context.BorrowRecords
            .AsNoTracking()
            .Where(record => record.Status == BorrowStatuses.Borrowed
                && record.ExpectedReturnDate < todayStartUtc);
        if (!isManager)
        {
            overdueBaseQuery = overdueBaseQuery.Where(record => record.UserId == userId);
        }

        var overdueBorrowRecords = await overdueBaseQuery.CountAsync(cancellationToken);
        var overdueRecords = await overdueBaseQuery
            .Include(record => record.User)
            .Include(record => record.Equipment)
            .Include(record => record.Details)
                .ThenInclude(detail => detail.Equipment)
            .AsSingleQuery()
            .OrderBy(record => record.ExpectedReturnDate)
            .Take(5)
            .ToListAsync(cancellationToken);
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
        var lowStockConsumableCount = 0;
        var warrantyExpiringSoon = 0;
        var lowStockConsumables = new List<object>();
        var borrowTrends = new List<object>();
        var teacherPendingApprovals = 0;
        var teacherPendingOwnRequests = 0;
        var teacherActiveBorrows = 0;
        DateTime? teacherNextReturnDate = null;
        var teacherNextReturnEquipment = string.Empty;
        var studentPendingRequests = 0;
        var studentApprovedRequests = 0;
        var studentActiveBorrows = 0;
        var studentReturnedBorrows = 0;
        DateTime? studentNextReturnDate = null;
        var studentNextReturnEquipment = string.Empty;
        var studentStatusCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        if (isManager)
        {
            totalUsers = await _context.Users.CountAsync(
                user => user.IsActive,
                cancellationToken);
            totalPenalties = await _context.Penalties
                .Where(penalty => penalty.Status == PenaltyStatuses.Unpaid)
                .SumAsync(penalty => penalty.Amount, cancellationToken);
            var borrowWork = await _context.BorrowRecords
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Pending = group.Count(record => record.Status == BorrowStatuses.Pending),
                    ToProcess = group.Count(record => record.Status == BorrowStatuses.Pending
                        || record.Status == BorrowStatuses.Approved
                        || record.Status == BorrowStatuses.ReturnProcessing)
                })
                .SingleOrDefaultAsync(cancellationToken);
            pendingBorrowRequests = borrowWork?.Pending ?? 0;
            borrowRequestsToProcess = borrowWork?.ToProcess ?? 0;

            var consumableWork = await _context.ConsumableRequests
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Pending = group.Count(request => request.Status == ConsumableRequestStatuses.Pending),
                    ToProcess = group.Count(request => request.Status == ConsumableRequestStatuses.Pending
                        || request.Status == ConsumableRequestStatuses.Approved)
                })
                .SingleOrDefaultAsync(cancellationToken);
            pendingConsumableRequests = consumableWork?.Pending ?? 0;
            consumableRequestsToProcess = consumableWork?.ToProcess ?? 0;
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
            var warrantySoon = await warrantySoonQuery
                .OrderBy(equipment => equipment.WarrantyExpiry)
                .Select(equipment => new
                {
                    equipment.Name,
                    equipment.WarrantyExpiry
                })
                .ToListAsync(cancellationToken);
            warrantyExpiringSoon = warrantySoon.Count;

            foreach (var equipment in warrantySoon.Take(5))
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
        else if (role == Roles.Teacher)
        {
            var teacherWork = await _context.BorrowRecords
                .AsNoTracking()
                .Where(record => record.UserId == userId || record.TeacherId == userId)
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    PendingApprovals = group.Count(record => record.TeacherId == userId
                        && record.Status == BorrowStatuses.TeacherPending),
                    PendingOwnRequests = group.Count(record => record.UserId == userId
                        && (record.Status == BorrowStatuses.TeacherPending
                            || record.Status == BorrowStatuses.Pending
                            || record.Status == BorrowStatuses.ProcessingApproval
                            || record.Status == BorrowStatuses.Approved)),
                    ActiveBorrows = group.Count(record => record.UserId == userId
                        && (record.Status == BorrowStatuses.Borrowed
                            || record.Status == BorrowStatuses.ReturnProcessing))
                })
                .SingleOrDefaultAsync(cancellationToken);
            teacherPendingApprovals = teacherWork?.PendingApprovals ?? 0;
            teacherPendingOwnRequests = teacherWork?.PendingOwnRequests ?? 0;
            teacherActiveBorrows = teacherWork?.ActiveBorrows ?? 0;

            var nextReturn = await _context.BorrowRecords
                .AsNoTracking()
                .Include(record => record.Equipment)
                .Include(record => record.Details)
                    .ThenInclude(detail => detail.Equipment)
                .Where(record => record.UserId == userId
                    && (record.Status == BorrowStatuses.Borrowed
                        || record.Status == BorrowStatuses.ReturnProcessing))
                .AsSingleQuery()
                .OrderBy(record => record.ExpectedReturnDate)
                .FirstOrDefaultAsync(cancellationToken);
            if (nextReturn != null)
            {
                teacherNextReturnDate = nextReturn.ExpectedReturnDate;
                teacherNextReturnEquipment = EquipmentLabel(nextReturn);
            }

            if (teacherPendingApprovals > 0)
            {
                alerts.Add(new
                {
                    Type = "teacher-pending-approvals",
                    Title = "Yêu cầu chờ bảo lãnh",
                    Message = $"Có {teacherPendingApprovals} yêu cầu của sinh viên cần bạn xem xét.",
                    Level = "warning"
                });
            }
        }

        else if (role == Roles.Student)
        {
            var studentBorrowStatusRows = await _context.BorrowRecords
                .AsNoTracking()
                .Where(record => record.UserId == userId)
                .GroupBy(record => record.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToListAsync(cancellationToken);
            studentStatusCounts = studentBorrowStatusRows.ToDictionary(
                item => item.Status,
                item => item.Count,
                StringComparer.Ordinal);

            studentPendingRequests = GetStatusCount(studentStatusCounts, BorrowStatuses.Pending)
                + GetStatusCount(studentStatusCounts, BorrowStatuses.TeacherPending)
                + GetStatusCount(studentStatusCounts, BorrowStatuses.ProcessingApproval);
            studentApprovedRequests = GetStatusCount(studentStatusCounts, BorrowStatuses.Approved);
            studentActiveBorrows = GetStatusCount(studentStatusCounts, BorrowStatuses.Borrowed)
                + GetStatusCount(studentStatusCounts, BorrowStatuses.ReturnProcessing);
            studentReturnedBorrows = GetStatusCount(studentStatusCounts, BorrowStatuses.Returned)
                + GetStatusCount(studentStatusCounts, BorrowStatuses.ReturnedDamaged);

            var nextReturn = await _context.BorrowRecords
                .AsNoTracking()
                .Include(record => record.Equipment)
                .Include(record => record.Details)
                    .ThenInclude(detail => detail.Equipment)
                .Where(record => record.UserId == userId
                    && (record.Status == BorrowStatuses.Borrowed
                        || record.Status == BorrowStatuses.ReturnProcessing))
                .AsSingleQuery()
                .OrderBy(record => record.ExpectedReturnDate)
                .FirstOrDefaultAsync(cancellationToken);
            if (nextReturn != null)
            {
                studentNextReturnDate = nextReturn.ExpectedReturnDate;
                studentNextReturnEquipment = EquipmentLabel(nextReturn);
            }
        }

        var payload = new
        {
            UpdatedAt = now,
            Counts = isManager
                ? new
                {
                    Total = equipmentCounts?.Total ?? 0,
                    Available = equipmentCounts?.Available ?? 0,
                    BorrowPending = equipmentCounts?.BorrowPending ?? 0,
                    Maintenance = equipmentCounts?.Maintenance ?? 0,
                    Borrowed = equipmentCounts?.Borrowed ?? 0,
                    Broken = equipmentCounts?.Broken ?? 0,
                    Missing = equipmentCounts?.Missing ?? 0,
                    Warranty = equipmentCounts?.Warranty ?? 0
                }
                : null,
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
            TeacherSummary = new
            {
                PendingApprovals = teacherPendingApprovals,
                PendingOwnRequests = teacherPendingOwnRequests,
                ActiveBorrows = teacherActiveBorrows,
                NextReturnDate = teacherNextReturnDate,
                NextReturnEquipment = teacherNextReturnEquipment
            },
            StudentSummary = new
            {
                PendingRequests = studentPendingRequests,
                ApprovedRequests = studentApprovedRequests,
                ActiveBorrows = studentActiveBorrows,
                ReturnedBorrows = studentReturnedBorrows,
                NextReturnDate = studentNextReturnDate,
                NextReturnEquipment = studentNextReturnEquipment,
                StatusCounts = new
                {
                    Pending = studentPendingRequests,
                    Approved = studentApprovedRequests,
                    Active = studentActiveBorrows,
                    Returned = studentReturnedBorrows,
                    Rejected = GetStatusCount(studentStatusCounts, BorrowStatuses.Rejected),
                    Cancelled = GetStatusCount(studentStatusCounts, BorrowStatuses.Cancelled),
                    Expired = GetStatusCount(studentStatusCounts, BorrowStatuses.Expired)
                }
            },
            PendingBorrowRequests = pendingBorrowRequests,
            PendingConsumableRequests = pendingConsumableRequests,
            BorrowRequestsToProcess = borrowRequestsToProcess,
            ConsumableRequestsToProcess = consumableRequestsToProcess,
            OverdueBorrowRecords = overdueBorrowRecords,
            LowStockConsumables = lowStockConsumableCount,
            WarrantyExpiringSoon = warrantyExpiringSoon,
            MaintenanceInProgress = equipmentCounts?.Maintenance ?? 0
        };

        _cache.Set(cacheKey, payload, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DashboardCacheDuration,
            Size = 1
        });
        return Ok(payload);
    }

    private sealed record DashboardActivity(
        string Type,
        string Message,
        DateTime Date,
        string Color);

    private static int GetStatusCount(
        IReadOnlyDictionary<string, int> counts,
        string status)
        => counts.TryGetValue(status, out var count) ? count : 0;

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
