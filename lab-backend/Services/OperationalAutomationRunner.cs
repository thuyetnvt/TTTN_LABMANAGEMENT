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
    private const string MaintenanceDueSoon = "MAINTENANCE_DUE_SOON";
    private const string ReturnDueSoon = "RETURN_DUE_SOON";
    private const string ReturnDueToday = "RETURN_DUE_TODAY";
    private const string ReturnOverdue = "RETURN_OVERDUE";
    private const string ReturnOverduePenalty = "RETURN_OVERDUE_PENALTY";
    private const string BorrowHoldExpired = "BORROW_HOLD_EXPIRED";
    private const string AutomaticOverduePenaltyReasonPrefix = "Tự động phạt trả quá hạn";
    private const decimal DefaultOverduePenaltyAmountPerDay = 10000m;

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
        await CheckUpcomingMaintenanceAsync(utcNow, cancellationToken);
        await GenerateDueMaintenanceAsync(utcNow, cancellationToken);
        await CreateOverduePenaltiesAsync(utcNow, cancellationToken);
        await CreateReturnRemindersAsync(utcNow, cancellationToken);
        if (_configuration.GetValue("Automation:SendEmailReminders", false))
        {
            await SendPendingReminderEmailsAsync(utcNow, cancellationToken);
            await SendPendingMaintenanceEmailsAsync(utcNow, cancellationToken);
        }
    }

    private async Task CreateOverduePenaltiesAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var amountPerDay = _configuration.GetValue(
            "Automation:OverduePenaltyAmountPerDay",
            DefaultOverduePenaltyAmountPerDay);
        if (amountPerDay <= 0) return;

        var today = VietnamTime.Today(utcNow);
        var todayStartUtc = VietnamTime.StartOfDayUtc(today);
        var records = await _context.BorrowRecords
            .AsNoTracking()
            .Include(record => record.Details)
            .Where(record => record.Status == BorrowStatuses.Borrowed
                && record.ExpectedReturnDate < todayStartUtc)
            .OrderBy(record => record.ExpectedReturnDate)
            .Take(200)
            .ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            var equipmentId = record.EquipmentId
                ?? record.Details.Select(detail => (int?)detail.EquipmentId).FirstOrDefault();
            if (!equipmentId.HasValue || equipmentId.Value <= 0) continue;

            var dueDate = VietnamTime.Date(record.ExpectedReturnDate);
            var daysOverdue = Math.Max(1, (today - dueDate).Days);
            var totalDue = amountPerDay * daysOverdue;
            var windowKey = today.ToString("yyyyMMdd");
            if (await DispatchExistsAsync(ReturnOverduePenalty, record.Id, windowKey, cancellationToken))
            {
                continue;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (await DispatchExistsAsync(ReturnOverduePenalty, record.Id, windowKey, cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    continue;
                }

                var dispatch = AddDispatch(
                    ReturnOverduePenalty,
                    nameof(BorrowRecord),
                    record.Id,
                    windowKey,
                    record.UserId);
                var automaticPenalties = await _context.Penalties
                    .Where(penalty => penalty.BorrowRecordId == record.Id
                        && penalty.Reason.StartsWith(AutomaticOverduePenaltyReasonPrefix))
                    .OrderByDescending(penalty => penalty.CreatedAt)
                    .ToListAsync(cancellationToken);
                var paidAmount = automaticPenalties
                    .Where(penalty => penalty.Status == PenaltyStatuses.Paid)
                    .Sum(penalty => penalty.Amount);
                var outstandingAmount = Math.Max(0m, totalDue - paidAmount);
                var unpaidPenalty = automaticPenalties
                    .FirstOrDefault(penalty => penalty.Status == PenaltyStatuses.Unpaid);
                var reason = $"{AutomaticOverduePenaltyReasonPrefix}: {daysOverdue} ngày (phiếu mượn #{record.Id})";

                if (unpaidPenalty is not null)
                {
                    unpaidPenalty.Amount = outstandingAmount;
                    unpaidPenalty.Reason = reason;
                    unpaidPenalty.EquipmentId = equipmentId.Value;
                }
                else if (outstandingAmount > 0)
                {
                    _context.Penalties.Add(new Penalty
                    {
                        UserId = record.UserId,
                        EquipmentId = equipmentId.Value,
                        BorrowRecordId = record.Id,
                        Reason = reason,
                        Amount = outstandingAmount,
                        Status = PenaltyStatuses.Unpaid,
                        CreatedAt = utcNow
                    });
                }

                dispatch.CompletedAt = utcNow;
                AddSystemAudit(
                    "AutoCreateOverduePenalty",
                    nameof(BorrowRecord),
                    record.Id,
                    new { record.ExpectedReturnDate, daysOverdue, Amount = outstandingAmount },
                    utcNow);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                _logger.LogWarning(
                    exception,
                    "Overdue penalty for borrow record {BorrowRecordId} was not committed.",
                    record.Id);
            }
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

    private async Task CheckUpcomingMaintenanceAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var targetDate = utcNow.AddDays(3);
        
        var upcomingSchedules = await _context.MaintenanceSchedules.AsNoTracking()
            .Include(s => s.Equipment)
            .Where(schedule => schedule.IsActive && schedule.NextDueAt > utcNow && schedule.NextDueAt <= targetDate)
            .OrderBy(schedule => schedule.NextDueAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var schedule in upcomingSchedules)
        {
            var dispatchKey = schedule.NextDueAt.ToString("yyyyMMdd");
            if (!await DispatchExistsAsync(MaintenanceDueSoon, schedule.Id, dispatchKey, cancellationToken))
            {
                AddDispatch(MaintenanceDueSoon, nameof(MaintenanceSchedule), schedule.Id, dispatchKey);
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
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
                        && (BorrowLockRules.EquipmentLockedBorrowStatuses.Contains(record.Status)
                            || record.Status == BorrowStatuses.Borrowed),
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
                var isOverdue = dispatch.JobType == ReturnOverdue;
                var isToday = dispatch.JobType == ReturnDueToday;
                
                var subject = isOverdue 
                    ? $"[KHẨN] Tài sản quá hạn trả - {record.ExpectedReturnDate:dd/MM/yyyy}"
                    : isToday 
                        ? $"[Lab] Hạn trả tài sản là hôm nay - {record.ExpectedReturnDate:dd/MM/yyyy}"
                        : $"[Lab] Sắp đến hạn trả tài sản - {record.ExpectedReturnDate:dd/MM/yyyy}";

                var overdueWarning = isOverdue 
                    ? $"<p style='color:red;'><strong>LƯU Ý: Tài sản của bạn đã quá hạn trả! Vui lòng hoàn trả ngay lập tức để tránh bị phạt theo quy định.</strong></p>" 
                    : "<p>Vui lòng sắp xếp thời gian hoàn trả đúng hạn.</p>";

                var htmlBody = $"<h3>Chào {username},</h3>" +
                               $"<p>Hệ thống LabManagement thông báo về tình trạng mượn tài sản của bạn:</p>" +
                               $"<ul><li>Tài sản: <strong>{equipmentNames}</strong></li>" +
                               $"<li>Hạn trả: <strong>{record.ExpectedReturnDate:dd/MM/yyyy}</strong></li></ul>" +
                               overdueWarning;

                await _emailService.SendEmailAsync(
                    record.User.Email,
                    subject,
                    htmlBody,
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
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SendPendingMaintenanceEmailsAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Clamp(_configuration.GetValue("Automation:EmailMaxAttempts", 3), 1, 10);
        var retryMinutes = Math.Clamp(_configuration.GetValue("Automation:EmailRetryMinutes", 60), 5, 1440);
        var retryBefore = utcNow.AddMinutes(-retryMinutes);
        
        var pending = await _context.AutomationDispatches
            .Where(item => item.JobType == MaintenanceDueSoon
                && item.EmailSentAt == null
                && item.Attempts < maxAttempts
                && (item.LastAttemptAt == null || item.LastAttemptAt <= retryBefore))
            .OrderBy(item => item.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (!pending.Any()) return;

        var managers = await _context.Users.AsNoTracking()
            .Where(u => (u.Role == Roles.LabHead || u.Role == Roles.DeputyLabHead) && !string.IsNullOrWhiteSpace(u.Email))
            .Select(u => new { u.Email, u.Username })
            .ToListAsync(cancellationToken);

        if (!managers.Any())
        {
            foreach (var dispatch in pending)
            {
                dispatch.Attempts = maxAttempts;
                dispatch.LastAttemptAt = utcNow;
                dispatch.LastError = "Không tìm thấy quản lý nào có email.";
            }
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        foreach (var dispatch in pending)
        {
            var schedule = await _context.MaintenanceSchedules.AsNoTracking()
                .Include(s => s.Equipment)
                .SingleOrDefaultAsync(s => s.Id == dispatch.EntityId, cancellationToken);

            if (schedule?.Equipment == null)
            {
                dispatch.Attempts = maxAttempts;
                dispatch.LastAttemptAt = utcNow;
                dispatch.LastError = "Không tìm thấy thông tin lịch bảo trì hoặc thiết bị.";
                await _context.SaveChangesAsync(cancellationToken);
                continue;
            }

            dispatch.Attempts++;
            dispatch.LastAttemptAt = utcNow;

            try
            {
                var equipmentName = WebUtility.HtmlEncode(schedule.Equipment.Name);
                var scheduleName = WebUtility.HtmlEncode(schedule.Name);
                
                var subject = $"[Lab] Sắp đến hạn bảo trì: {equipmentName}";
                var htmlBody = $"<h3>Thông báo Bảo trì</h3>" +
                               $"<p>Hệ thống ghi nhận lịch bảo trì sắp đến hạn:</p>" +
                               $"<ul>" +
                               $"<li>Thiết bị: <strong>{equipmentName}</strong></li>" +
                               $"<li>Kế hoạch: <strong>{scheduleName}</strong></li>" +
                               $"<li>Ngày đến hạn: <strong>{schedule.NextDueAt:dd/MM/yyyy}</strong></li>" +
                               $"</ul>" +
                               $"<p>Vui lòng chuẩn bị và kiểm tra thiết bị.</p>";

                foreach (var manager in managers)
                {
                    await _emailService.SendEmailAsync(
                        manager.Email!,
                        subject,
                        htmlBody,
                        cancellationToken);
                }

                dispatch.EmailSentAt = utcNow;
                dispatch.LastError = string.Empty;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                dispatch.LastError = exception.Message.Length > 2000
                    ? exception.Message[..2000]
                    : exception.Message;
                _logger.LogWarning(exception, "Automated maintenance reminder email failed for schedule {ScheduleId}.", schedule.Id);
            }
        }
        await _context.SaveChangesAsync(cancellationToken);
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
