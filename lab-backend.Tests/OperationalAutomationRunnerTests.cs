using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LabManagementAPI.Tests;

public sealed class OperationalAutomationRunnerTests
{
    [Fact]
    public async Task Due_schedule_is_generated_once_and_next_due_moves_to_future()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            var now = new DateTime(2026, 9, 1, 2, 0, 0, DateTimeKind.Utc);
            context.Users.Add(new User { Id = 7, Username = "manager", Role = Roles.LabHead, IsActive = true });
            context.Equipments.Add(new Equipment
            {
                Id = 1,
                AssetCode = "EQ-001",
                QrToken = "qr-001",
                Name = "Máy hiện sóng",
                Serial = "SN-001",
                Model = "M1",
                Location = "Lab",
                Status = EquipmentStatuses.Available
            });
            context.MaintenanceSchedules.Add(new MaintenanceSchedule
            {
                Id = 10,
                EquipmentId = 1,
                Name = "Hiệu chuẩn tháng",
                IntervalDays = 1,
                IntervalUnit = "MONTH",
                NextDueAt = now.AddDays(-2),
                CreatedByUserId = 7
            });
            await context.SaveChangesAsync();
            var notifications = new RecordingNotificationService();
            var runner = CreateRunner(context, notifications);

            await runner.RunOnceAsync(now);
            await runner.RunOnceAsync(now.AddMinutes(1));

            Assert.Single(await context.MaintenanceRecords.AsNoTracking().ToListAsync());
            Assert.Single(await context.AutomationDispatches.AsNoTracking()
                .Where(item => item.JobType == "MAINTENANCE_GENERATE").ToListAsync());
            Assert.True((await context.MaintenanceSchedules.AsNoTracking().SingleAsync()).NextDueAt > now);
            Assert.Equal(EquipmentStatuses.MaintenanceInProgress,
                (await context.Equipments.AsNoTracking().SingleAsync()).Status);
            Assert.Single(notifications.ManagerNotifications);
        }
    }

    [Fact]
    public async Task Overdue_reminder_is_sent_once_per_day()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            var now = new DateTime(2026, 9, 1, 2, 0, 0, DateTimeKind.Utc);
            context.Users.Add(new User
            {
                Id = 1,
                Username = "student",
                Email = "student@lab.local",
                Role = Roles.Student,
                IsActive = true
            });
            context.Equipments.Add(new Equipment
            {
                Id = 1,
                AssetCode = "EQ-001",
                QrToken = "qr-001",
                Name = "ESP32",
                Serial = "SN-001",
                Model = "M1",
                Location = "Lab",
                Status = EquipmentStatuses.Borrowed
            });
            context.BorrowRecords.Add(new BorrowRecord
            {
                Id = 20,
                UserId = 1,
                EquipmentId = 1,
                BorrowDate = now.AddDays(-5),
                ExpectedReturnDate = now.AddDays(-1),
                Purpose = "Thực hành",
                Status = BorrowStatuses.Borrowed
            });
            await context.SaveChangesAsync();
            var notifications = new RecordingNotificationService();
            var runner = CreateRunner(context, notifications);

            await runner.RunOnceAsync(now);
            await runner.RunOnceAsync(now.AddHours(2));
            Assert.Single(notifications.UserNotifications);

            await runner.RunOnceAsync(now.AddDays(1));
            Assert.Equal(2, notifications.UserNotifications.Count);
            Assert.Equal(2, await context.AutomationDispatches.AsNoTracking()
                .CountAsync(item => item.JobType == "RETURN_OVERDUE"));
        }
    }

    [Fact]
    public async Task Due_schedule_for_borrowed_equipment_is_blocked_without_duplicate_daily_alerts()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            var now = new DateTime(2026, 9, 1, 2, 0, 0, DateTimeKind.Utc);
            context.Users.AddRange(
                new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
                new User { Id = 7, Username = "manager", Role = Roles.LabHead, IsActive = true });
            context.Equipments.Add(new Equipment
            {
                Id = 1,
                AssetCode = "EQ-001",
                QrToken = "qr-001",
                Name = "Gateway",
                Serial = "SN-001",
                Model = "M1",
                Location = "Lab",
                Status = EquipmentStatuses.Borrowed
            });
            context.BorrowRecords.Add(new BorrowRecord
            {
                Id = 30,
                UserId = 1,
                EquipmentId = 1,
                BorrowDate = now.AddDays(-1),
                ExpectedReturnDate = now.AddDays(2),
                Purpose = "Demo",
                Status = BorrowStatuses.Borrowed
            });
            context.MaintenanceSchedules.Add(new MaintenanceSchedule
            {
                Id = 10,
                EquipmentId = 1,
                Name = "Kiểm tra định kỳ",
                IntervalDays = 30,
                NextDueAt = now.AddDays(-1),
                CreatedByUserId = 7
            });
            await context.SaveChangesAsync();
            var notifications = new RecordingNotificationService();
            var runner = CreateRunner(context, notifications);

            await runner.RunOnceAsync(now);
            await runner.RunOnceAsync(now.AddHours(1));

            Assert.Empty(await context.MaintenanceRecords.AsNoTracking().ToListAsync());
            Assert.Single(notifications.ManagerNotifications);
            Assert.Single(await context.AutomationDispatches.AsNoTracking()
                .Where(item => item.JobType == "MAINTENANCE_BLOCKED").ToListAsync());
        }
    }

    [Fact]
    public async Task Overdue_penalty_is_created_once_per_day_and_accumulates()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            var now = new DateTime(2026, 9, 1, 2, 0, 0, DateTimeKind.Utc);
            context.Users.Add(new User
            {
                Id = 1,
                Username = "student",
                Role = Roles.Student,
                IsActive = true
            });
            context.Equipments.Add(new Equipment
            {
                Id = 1,
                AssetCode = "EQ-001",
                QrToken = "qr-001",
                Name = "ESP32",
                Serial = "SN-001",
                Model = "M1",
                Location = "Lab",
                Status = EquipmentStatuses.Borrowed
            });
            context.BorrowRecords.Add(new BorrowRecord
            {
                Id = 21,
                UserId = 1,
                EquipmentId = 1,
                BorrowDate = now.AddDays(-5),
                ExpectedReturnDate = now.AddDays(-1),
                Purpose = "Thực hành",
                Status = BorrowStatuses.Borrowed
            });
            await context.SaveChangesAsync();
            var runner = CreateRunner(context, new RecordingNotificationService());

            await runner.RunOnceAsync(now);
            await runner.RunOnceAsync(now.AddHours(2));

            var penalty = await context.Penalties.AsNoTracking().SingleAsync();
            Assert.Equal(10000m, penalty.Amount);
            Assert.Equal(PenaltyStatuses.Unpaid, penalty.Status);
            Assert.Single(await context.AutomationDispatches.AsNoTracking()
                .Where(item => item.JobType == "RETURN_OVERDUE_PENALTY")
                .ToListAsync());

            await runner.RunOnceAsync(now.AddDays(1));

            penalty = await context.Penalties.AsNoTracking().SingleAsync();
            Assert.Equal(20000m, penalty.Amount);
            Assert.Single(await context.Penalties.AsNoTracking().ToListAsync());
            Assert.Equal(2, await context.AutomationDispatches.AsNoTracking()
                .CountAsync(item => item.JobType == "RETURN_OVERDUE_PENALTY"));
        }
    }

    [Fact]
    public async Task Approved_hold_expires_once_and_releases_reserved_equipment()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            var now = new DateTime(2026, 9, 1, 2, 0, 0, DateTimeKind.Utc);
            context.Users.AddRange(
                new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
                new User { Id = 7, Username = "manager", Role = Roles.LabHead, IsActive = true });
            context.Equipments.Add(new Equipment
            {
                Id = 1,
                AssetCode = "EQ-001",
                QrToken = "qr-001",
                Name = "ESP32",
                Serial = "SN-001",
                Model = "M1",
                Location = "Lab",
                Status = EquipmentStatuses.BorrowPending
            });
            context.BorrowRecords.Add(new BorrowRecord
            {
                Id = 40,
                UserId = 1,
                BorrowDate = now.AddHours(-5),
                ExpectedReturnDate = now.AddDays(3),
                HoldExpiresAt = now.AddMinutes(-1),
                Purpose = "Chờ bàn giao",
                Status = BorrowStatuses.Approved,
                Details = [new BorrowRequestDetail { EquipmentId = 1, Quantity = 1, Status = BorrowStatuses.Approved }]
            });
            await context.SaveChangesAsync();
            var notifications = new RecordingNotificationService();
            var runner = CreateRunner(context, notifications);

            await runner.RunOnceAsync(now);
            await runner.RunOnceAsync(now.AddMinutes(1));

            var record = await context.BorrowRecords.AsNoTracking().Include(item => item.Details).SingleAsync();
            Assert.Equal(BorrowStatuses.Expired, record.Status);
            Assert.NotEmpty(record.CancellationReason);
            Assert.Equal(BorrowStatuses.Expired, record.Details.Single().Status);
            Assert.Equal(EquipmentStatuses.Available, (await context.Equipments.AsNoTracking().SingleAsync()).Status);
            Assert.Single(notifications.UserNotifications, item => item.Type == "BORROW_HOLD_EXPIRED");
            Assert.Single(await context.AutomationDispatches.AsNoTracking()
                .Where(item => item.JobType == "BORROW_HOLD_EXPIRED").ToListAsync());
        }
    }

    [Fact]
    public async Task Approved_hold_with_handover_is_not_expired()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            var now = new DateTime(2026, 9, 1, 2, 0, 0, DateTimeKind.Utc);
            context.Users.AddRange(
                new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
                new User { Id = 7, Username = "manager", Role = Roles.LabHead, IsActive = true });
            context.Equipments.Add(new Equipment
            {
                Id = 1,
                AssetCode = "EQ-001",
                QrToken = "qr-001",
                Name = "ESP32",
                Serial = "SN-001",
                Model = "M1",
                Location = "Lab",
                Status = EquipmentStatuses.BorrowPending
            });
            context.BorrowRecords.Add(new BorrowRecord
            {
                Id = 41,
                UserId = 1,
                BorrowDate = now.AddHours(-5),
                ExpectedReturnDate = now.AddDays(3),
                HoldExpiresAt = now.AddMinutes(-1),
                Purpose = "Đã lập bàn giao",
                Status = BorrowStatuses.Approved,
                Details = [new BorrowRequestDetail { EquipmentId = 1, Quantity = 1, Status = BorrowStatuses.Approved }]
            });
            context.HandoverRecords.Add(new HandoverRecord
            {
                Id = 41,
                Code = "BH-TEST-41",
                BorrowRecordId = 41,
                HandedOverByUserId = 7,
                ReceivedByUserId = 1,
                Items = [new HandoverItem { EquipmentId = 1, Condition = EquipmentStatuses.Available }]
            });
            await context.SaveChangesAsync();
            var notifications = new RecordingNotificationService();
            var runner = CreateRunner(context, notifications);

            await runner.RunOnceAsync(now);

            Assert.Equal(BorrowStatuses.Approved, (await context.BorrowRecords.AsNoTracking().SingleAsync()).Status);
            Assert.Equal(EquipmentStatuses.BorrowPending, (await context.Equipments.AsNoTracking().SingleAsync()).Status);
            Assert.DoesNotContain(notifications.UserNotifications, item => item.Type == "BORROW_HOLD_EXPIRED");
        }
    }

    private static OperationalAutomationRunner CreateRunner(
        AppDbContext context,
        RecordingNotificationService notifications)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Automation:ReturnReminderDaysBefore"] = "3",
                ["Automation:OverduePenaltyAmountPerDay"] = "10000",
                ["Automation:SendEmailReminders"] = "false"
            })
            .Build();
        return new OperationalAutomationRunner(
            context,
            notifications,
            new NoopEmailService(),
            configuration,
            NullLogger<OperationalAutomationRunner>.Instance);
    }

    private static AppDbContext CreateContext(out SqliteConnection connection)
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class NoopEmailService : IEmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class RecordingNotificationService : INotificationService
    {
        public List<(int UserId, string Type)> UserNotifications { get; } = [];
        public List<string> ManagerNotifications { get; } = [];

        public Task NotifyUserAsync(int userId, string type, string title, string message, string url, CancellationToken cancellationToken)
        {
            UserNotifications.Add((userId, type));
            return Task.CompletedTask;
        }

        public Task NotifyUsersAsync(IEnumerable<int> userIds, string type, string title, string message, string url, CancellationToken cancellationToken)
        {
            UserNotifications.AddRange(userIds.Select(userId => (userId, type)));
            return Task.CompletedTask;
        }

        public Task NotifyManagersAsync(string type, string title, string message, string url, CancellationToken cancellationToken)
        {
            ManagerNotifications.Add(type);
            return Task.CompletedTask;
        }
    }
}
