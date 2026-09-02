using System.Net;
using System.Text.Json;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LabManagementAPI.Services;

public sealed class OperationalAutomationRunner
{
    private const string MaintenanceGenerate = "MAINTENANCE_GENERATE";
    private const string MaintenanceBlocked = "MAINTENANCE_BLOCKED";
    private const string ReturnDueSoon = "RETURN_DUE_SOON";
    private const string ReturnDueToday = "RETURN_DUE_TODAY";
    private const string ReturnOverdue = "RETURN_OVERDUE";
    private const string BorrowHoldExpired = "BORROW_HOLD_EXPIRED";

    private readonly AppDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OperationalAutomationRunner> _logger;

    public OperationalAutomationRunner(
        AppDbContext context,
        INotificationService notifications,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<OperationalAutomationRunner> logger)
    {
        _context = context;
        _notifications = notifications;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunOnceAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        utcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        await ExpireApprovedHoldsAsync(utcNow, cancellationToken);
        await GenerateDueMaintenanceAsync(utcNow, cancellationToken);
        await CreateReturnRemindersAsync(utcNow, cancellationToken);
        if (_configuration.GetValue("Automation:SendEmailReminders", false))
        {
            await SendPendingReminderEmailsAsync(utcNow, cancellationToken);
        }
    }

    private async Task ExpireApprovedHoldsAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var candidates = await _context.BorrowRecords
            .AsNoTracking()
            .Include(record => record.User)
            .Include(record => record.Equipment)
            .Include(record => record.Details)
                .ThenInclude(detail => detail.Equipment)
            .Include(record => record.StatusHistory)
            .Where(record => record.Status == BorrowStatuses.Approved
                && !_context.HandoverRecords.Any(handover => handover.BorrowRecordId == record.Id))
            .OrderBy(record => record.HoldExpiresAt ?? record.BorrowDate)
            .Take(200)
            .ToListAsync(cancellationToken);
        var holdDurationHours = GetApprovedHoldHours();

        foreach (var candidate in candidates)
        {
            var expiry = ResolveHoldExpiry(candidate, holdDurationHours);
            if (expiry > utcNow) continue;

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var record = await _context.BorrowRecords
                    .Include(item => item.Details)
                    .SingleOrDefaultAsync(item => item.Id == candidate.Id, cancellationToken);
                if (record is null || record.Status != BorrowStatuses.Approved)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                if (await _context.HandoverRecords.AnyAsync(
                    handover => handover.BorrowRecordId == record.Id,
                    cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                expiry = ResolveHoldExpiry(record, holdDurationHours);
                if (expiry > utcNow)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                var equipmentIds = record.Details
                    .Select(detail => detail.EquipmentId)
                    .Append(record.EquipmentId.GetValueOrDefault())
                    .Where(equipmentId => equipmentId > 0)
                    .Distinct()
                    .ToArray();
                var updated = await _context.BorrowRecords
                    .Where(item => item.Id == record.Id
                        && item.Status == BorrowStatuses.Approved
                        && !_context.HandoverRecords.Any(handover => handover.BorrowRecordId == record.Id))
                    .ExecuteUpdateAsync(
                        updates => updates
                            .SetProperty(item => item.Status, BorrowStatuses.Expired)
                            .SetProperty(item => item.CancellationReason, "Tự động hết hạn giữ chỗ sau khi được duyệt nhưng chưa lập biên bản bàn giao.")
                            .SetProperty(item => item.CancelledAt, utcNow)
                            .SetProperty(item => item.CancelledByUserId, (int?)null)
                            .SetProperty(item => item.HoldExpiresAt, expiry),
                        cancellationToken);
                if (updated == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                await _context.BorrowRequestDetails
                    .Where(detail => detail.BorrowRecordId == record.Id
                        && detail.Status == BorrowStatuses.Approved)
                    .ExecuteUpdateAsync(
                        updates => updates.SetProperty(detail => detail.Status, BorrowStatuses.Expired),
                        cancellationToken);

                if (equipmentIds.Length > 0)
                {
                    var released = await _context.Equipments
                        .Where(item => equipmentIds.Contains(item.Id)
                            && item.Status == EquipmentStatuses.BorrowPending)
                        .ExecuteUpdateAsync(
                            updates => updates.SetProperty(item => item.Status, EquipmentStatuses.Available),
                            cancellationToken);
                    if (released != equipmentIds.Length)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _logger.LogWarning(
                            "Borrow hold expiration for record {BorrowRecordId} found inconsistent equipment state.",
                            record.Id);
                        continue;
                    }
                }

                _context.BorrowStatusHistories.Add(new BorrowStatusHistory
                {
                    BorrowRecordId = record.Id,
                    FromStatus = BorrowStatuses.Approved,
                    ToStatus = BorrowStatuses.Expired,
                    Note = $"Tự động hết hạn giữ chỗ lúc {expiry:O} vì chưa lập biên bản bàn giao.",
                    ChangedByUserId = null
                });
                var dispatch = AddDispatch(
                    BorrowHoldExpired,
                    nameof(BorrowRecord),
                    record.Id,
                    expiry.ToString("O"),
                    record.UserId);
                AddSystemAudit(
                    "ExpireBorrowHold",
                    nameof(BorrowRecord),
                    record.Id,
                    new { HoldExpiresAt = expiry, EquipmentIds = equipmentIds },
                    utcNow);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                await _notifications.NotifyUserAsync(
                    record.UserId,
                    BorrowHoldExpired,
                    "Phiếu mượn đã hết hạn giữ chỗ",
                    "Phiếu mượn đã hết hạn vì chưa lập biên bản bàn giao; tài sản đã được trả về trạng thái sẵn sàng.",
                    "/dashboard/borrow-history",
                    cancellationToken);
                dispatch.CompletedAt = utcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                _logger.LogWarning(
                    exception,
                    "Borrow hold expiration for record {BorrowRecordId} was not committed.",
                    candidate.Id);
            }
        }
    }

    private int GetApprovedHoldHours()
        => Math.Clamp(_configuration.GetValue("Borrow:ApprovedHoldHours", 24), 1, 720);

    private static DateTime ResolveHoldExpiry(BorrowRecord record, int holdDurationHours)
    {
        if (record.HoldExpiresAt.HasValue)
        {
            return DateTime.SpecifyKind(record.HoldExpiresAt.Value, DateTimeKind.Utc);
        }

        var approvedAt = record.StatusHistory
            .Where(history => history.ToStatus == BorrowStatuses.Approved)
            .OrderByDescending(history => history.CreatedAt)
            .Select(history => (DateTime?)history.CreatedAt)
            .FirstOrDefault()
            ?? record.BorrowDate;
        return DateTime.SpecifyKind(approvedAt, DateTimeKind.Utc).AddHours(holdDurationHours);
    }

    private async Task GenerateDueMaintenanceAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var scheduleIds = await _context.MaintenanceSchedules.AsNoTracking()
            .Where(schedule => schedule.IsActive && schedule.NextDueAt <= utcNow)
            .OrderBy(schedule => schedule.NextDueAt)
            .Select(schedule => schedule.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var scheduleId in scheduleIds)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var schedule = await _context.MaintenanceSchedules
                    .Include(item => item.Equipment)
                    .SingleOrDefaultAsync(item => item.Id == scheduleId, cancellationToken);
                if (schedule?.Equipment is null || !schedule.IsActive || schedule.NextDueAt > utcNow)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                var hasActiveBorrow = await _context.BorrowRecords.AsNoTracking()
                    .AnyAsync(record => (record.EquipmentId == schedule.EquipmentId
                            || record.Details.Any(detail => detail.EquipmentId == schedule.EquipmentId))
                        && (record.Status == BorrowStatuses.Approved || record.Status == BorrowStatuses.Borrowed),
                        cancellationToken);
                var hasActiveMaintenance = await _context.MaintenanceRecords.AsNoTracking()
                    .AnyAsync(record => record.EquipmentId == schedule.EquipmentId
                        && (record.Status == MaintenanceStatuses.InProgress
                            || record.Status == MaintenanceStatuses.Completing),
                        cancellationToken);

                if (hasActiveBorrow
                    || schedule.Equipment.Status is EquipmentStatuses.Borrowed or EquipmentStatuses.BorrowPending
                    || hasActiveMaintenance)
                {
                    var dailyWindow = utcNow.ToString("yyyyMMdd");
                    if (await DispatchExistsAsync(MaintenanceBlocked, schedule.Id, dailyWindow, cancellationToken))
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        continue;
                    }

                    var blockedDispatch = AddDispatch(MaintenanceBlocked, nameof(MaintenanceSchedule), schedule.Id, dailyWindow);
                    await _context.SaveChangesAsync(cancellationToken);
                    await _notifications.NotifyManagersAsync(
                        MaintenanceBlocked,
                        "Lịch bảo trì đến hạn nhưng chưa thể tạo phiếu",
                        $"Kế hoạch {schedule.Name} của {schedule.Equipment.Name} đang bị chặn vì thiết bị đang mượn hoặc đã có phiếu bảo trì.",
                        "/dashboard/maintenance-schedules",
                        cancellationToken);
                    blockedDispatch.CompletedAt = utcNow;
                    AddSystemAudit("AutomationBlocked", nameof(MaintenanceSchedule), schedule.Id,
                        new { schedule.EquipmentId, schedule.NextDueAt, hasActiveBorrow, hasActiveMaintenance }, utcNow);
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    continue;
                }

                var generationWindow = schedule.NextDueAt.ToUniversalTime().ToString("O");
                if (await DispatchExistsAsync(MaintenanceGenerate, schedule.Id, generationWindow, cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                var dispatch = AddDispatch(MaintenanceGenerate, nameof(MaintenanceSchedule), schedule.Id, generationWindow);
                var maintenance = new MaintenanceRecord
                {
                    EquipmentId = schedule.EquipmentId,
                    MaintenanceDate = utcNow,
                    Description = $"Theo kế hoạch tự động: {schedule.Name}",
                    Cost = 0,
                    PerformedBy = "Theo kế hoạch tự động",
                    Status = MaintenanceStatuses.InProgress,
                    Checklist = schedule.Checklist,
                    ActiveEquipmentKey = $"EQ:{schedule.EquipmentId}"
                };
                _context.MaintenanceRecords.Add(maintenance);
                schedule.Equipment.Status = EquipmentStatuses.MaintenanceInProgress;
                schedule.LastGeneratedAt = utcNow;
                schedule.NextDueAt = GetNextFutureDue(
                    schedule.NextDueAt,
                    schedule.IntervalDays,
                    schedule.IntervalUnit,
                    utcNow);
                schedule.UpdatedAt = utcNow;
                await _context.SaveChangesAsync(cancellationToken);
                await _notifications.NotifyManagersAsync(
                    "MAINTENANCE_SCHEDULE_GENERATED",
                    "Đã tự động tạo nhiệm vụ bảo trì định kỳ",
                    $"Kế hoạch {schedule.Name} đã sinh phiếu #{maintenance.Id}.",
                    "/dashboard/maintenance",
                    cancellationToken);
                dispatch.CompletedAt = utcNow;
                AddSystemAudit("AutoGenerateMaintenance", nameof(MaintenanceSchedule), schedule.Id,
                    new { maintenance.Id, schedule.EquipmentId, schedule.NextDueAt }, utcNow);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                _logger.LogWarning(exception, "Maintenance automation for schedule {ScheduleId} was not committed.", scheduleId);
            }
        }
    }

    private async Task CreateReturnRemindersAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var reminderDays = Math.Clamp(_configuration.GetValue("Automation:ReturnReminderDaysBefore", 3), 0, 30);
        var today = VietnamTime.Today(utcNow);
        var reminderLimit = today.AddDays(reminderDays + 1);
        var reminderLimitUtc = VietnamTime.StartOfDayUtc(reminderLimit);
        var records = await _context.BorrowRecords.AsNoTracking()
            .Include(record => record.User)
            .Include(record => record.Equipment)
            .Include(record => record.Details)
                .ThenInclude(detail => detail.Equipment)
            .Where(record => record.Status == BorrowStatuses.Borrowed
                && record.ExpectedReturnDate < reminderLimitUtc)
            .OrderBy(record => record.ExpectedReturnDate)
            .Take(200)
            .ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            var dueDate = VietnamTime.Date(record.ExpectedReturnDate);
            var jobType = dueDate < today
                ? ReturnOverdue
                : dueDate == today ? ReturnDueToday : ReturnDueSoon;
            var windowKey = jobType == ReturnOverdue
                ? today.ToString("yyyyMMdd")
                : dueDate.ToString("yyyyMMdd");
            if (await DispatchExistsAsync(jobType, record.Id, windowKey, cancellationToken)) continue;

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (await DispatchExistsAsync(jobType, record.Id, windowKey, cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                var dispatch = AddDispatch(jobType, nameof(BorrowRecord), record.Id, windowKey, record.UserId);
                await _context.SaveChangesAsync(cancellationToken);
                var equipmentNames = GetEquipmentNames(record);
                var daysOverdue = Math.Max(0, (today - dueDate).Days);
                var title = jobType switch
                {
                    ReturnOverdue => $"Phiếu mượn đã quá hạn {daysOverdue} ngày",
                    ReturnDueToday => "Phiếu mượn đến hạn trả hôm nay",
                    _ => "Phiếu mượn sắp đến hạn trả"
                };
                var message = $"{equipmentNames}. Hạn trả: {record.ExpectedReturnDate:dd/MM/yyyy}.";
                await _notifications.NotifyUserAsync(
                    record.UserId,
                    jobType,
                    title,
                    message,
                    "/dashboard/borrow-history",
                    cancellationToken);
                dispatch.CompletedAt = utcNow;
                AddSystemAudit("AutoReturnReminder", nameof(BorrowRecord), record.Id,
                    new { jobType, record.ExpectedReturnDate, record.UserId }, utcNow);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                _logger.LogWarning(exception, "Return reminder for record {BorrowRecordId} was not committed.", record.Id);
            }
        }
    }

    private async Task SendPendingReminderEmailsAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Clamp(_configuration.GetValue("Automation:EmailMaxAttempts", 3), 1, 10);
        var retryMinutes = Math.Clamp(_configuration.GetValue("Automation:EmailRetryMinutes", 60), 5, 1440);
        var retryBefore = utcNow.AddMinutes(-retryMinutes);
        var pending = await _context.AutomationDispatches
            .Where(item => (item.JobType == ReturnDueSoon || item.JobType == ReturnDueToday || item.JobType == ReturnOverdue)
                && item.EmailSentAt == null
                && item.Attempts < maxAttempts
                && (item.LastAttemptAt == null || item.LastAttemptAt <= retryBefore))
            .OrderBy(item => item.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var dispatch in pending)
        {
            var record = await _context.BorrowRecords.AsNoTracking()
                .Include(item => item.User)
                .Include(item => item.Equipment)
                .Include(item => item.Details).ThenInclude(detail => detail.Equipment)
                .SingleOrDefaultAsync(item => item.Id == dispatch.EntityId, cancellationToken);
            if (record?.User is null || string.IsNullOrWhiteSpace(record.User.Email))
            {
                dispatch.Attempts = maxAttempts;
                dispatch.LastAttemptAt = utcNow;
                dispatch.LastError = "Người mượn chưa có email.";
                await _context.SaveChangesAsync(cancellationToken);
                continue;
            }

            dispatch.Attempts++;
            dispatch.LastAttemptAt = utcNow;
            try
            {
                var username = WebUtility.HtmlEncode(record.User.Username);
                var equipmentNames = WebUtility.HtmlEncode(GetEquipmentNames(record));
                await _emailService.SendEmailAsync(
                    record.User.Email,
                    $"[Lab] Nhắc trả tài sản - hạn {record.ExpectedReturnDate:dd/MM/yyyy}",
                    $"<h3>Chào {username},</h3><p>Bạn đang mượn <strong>{equipmentNames}</strong>.</p><p>Hạn trả: <strong>{record.ExpectedReturnDate:dd/MM/yyyy}</strong>.</p><p>Vui lòng hoàn trả đúng hạn.</p>",
                    cancellationToken);
                dispatch.EmailSentAt = utcNow;
                dispatch.LastError = string.Empty;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                dispatch.LastError = exception.Message.Length > 2000
                    ? exception.Message[..2000]
                    : exception.Message;
                _logger.LogWarning(exception, "Automated reminder email failed for borrow record {BorrowRecordId}.", record.Id);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private Task<bool> DispatchExistsAsync(string jobType, int entityId, string windowKey, CancellationToken cancellationToken)
        => _context.AutomationDispatches.AsNoTracking().AnyAsync(
            item => item.JobType == jobType && item.EntityId == entityId && item.WindowKey == windowKey,
            cancellationToken);

    private AutomationDispatch AddDispatch(string jobType, string entityType, int entityId, string windowKey, int? userId = null)
    {
        var dispatch = new AutomationDispatch
        {
            JobType = jobType,
            EntityType = entityType,
            EntityId = entityId,
            WindowKey = windowKey,
            UserId = userId
        };
        _context.AutomationDispatches.Add(dispatch);
        return dispatch;
    }

    private void AddSystemAudit(string action, string entityType, int entityId, object details, DateTime utcNow)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Username = "system",
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            Details = JsonSerializer.Serialize(details),
            IpAddress = "127.0.0.1",
            CreatedAt = utcNow
        });
    }

    private static string GetEquipmentNames(BorrowRecord record)
    {
        var names = record.Details
            .Select(detail => detail.Equipment?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToList();
        if (names.Count == 0 && !string.IsNullOrWhiteSpace(record.Equipment?.Name)) names.Add(record.Equipment.Name);
        return names.Count == 0 ? "tài sản phòng lab" : string.Join(", ", names);
    }

    public static DateTime GetNextFutureDue(DateTime currentDue, int amount, string unit, DateTime utcNow)
    {
        var next = currentDue;
        do
        {
            next = unit.ToUpperInvariant() switch
            {
                "WEEK" => next.AddDays(amount * 7),
                "MONTH" => next.AddMonths(amount),
                "QUARTER" => next.AddMonths(amount * 3),
                "YEAR" => next.AddYears(amount),
                _ => next.AddDays(amount)
            };
        } while (next <= utcNow);
        return next;
    }
}
