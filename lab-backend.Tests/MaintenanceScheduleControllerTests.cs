using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using LabManagementAPI.Controllers;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LabManagementAPI.Tests;

public sealed class MaintenanceScheduleControllerTests
{
    [Fact]
    public async Task Create_rejects_unknown_equipment()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);

        var result = await controller.Create(new MaintenanceScheduleController.ScheduleDto
        {
            EquipmentId = 99,
            Name = "Hiệu chuẩn hàng quý",
            IntervalDays = 90,
            NextDueAt = DateTime.UtcNow.AddDays(90)
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("không tồn tại", badRequest.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Generate_creates_maintenance_record_and_advances_next_due_date()
    {
        await using var context = CreateContext();
        var originalDueAt = DateTime.UtcNow.AddDays(-1);
        context.Equipments.Add(new Equipment
        {
            Id = 1,
            AssetCode = "IOT-001",
            QrToken = "qr-001",
            Name = "Gateway",
            Model = "GW-1",
            Serial = "SN-001",
            Location = "Phòng Lab"
        });
        context.MaintenanceSchedules.Add(new MaintenanceSchedule
        {
            Id = 1,
            EquipmentId = 1,
            Name = "Kiểm tra định kỳ",
            IntervalDays = 30,
            NextDueAt = originalDueAt,
            CreatedByUserId = 7
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.GenerateMaintenanceRecord(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var record = await context.MaintenanceRecords.SingleAsync();
        var schedule = await context.MaintenanceSchedules.SingleAsync();
        var equipment = await context.Equipments.SingleAsync();
        Assert.Equal(MaintenanceStatuses.InProgress, record.Status);
        Assert.Equal(EquipmentStatuses.MaintenanceInProgress, equipment.Status);
        Assert.NotNull(schedule.LastGeneratedAt);
        Assert.Equal(originalDueAt.AddDays(30), schedule.NextDueAt);
        Assert.True(schedule.NextDueAt > DateTime.UtcNow);
    }

    [Fact]
    public void Business_status_map_contains_stable_codes_for_legacy_values()
    {
        Assert.Equal(BorrowStatuses.Pending, StatusCodeMap.LegacyMap["Chờ duyệt"]);
        Assert.Equal(MaintenanceStatuses.Completed, StatusCodeMap.LegacyMap["Hoàn tất"]);
        Assert.Equal(PenaltyStatuses.Paid, StatusCodeMap.LegacyMap["Đã thanh toán"]);
    }

    [Fact]
    public async Task Student_must_select_a_teacher_for_multi_item_borrow_request()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true });
        context.Equipments.AddRange(
            new Equipment { Id = 1, AssetCode = "IOT-001", QrToken = "qr-001", Name = "ESP32", Model = "ESP", Serial = "SN-001", Location = "Lab" },
            new Equipment { Id = 2, AssetCode = "IOT-002", QrToken = "qr-002", Name = "Gateway", Model = "GW", Serial = "SN-002", Location = "Lab" });
        await context.SaveChangesAsync();
        var controller = CreateBorrowController(context, 1, Roles.Student);

        var result = await controller.CreateRequest(new BorrowController.BorrowRequestDto
        {
            ExpectedReturnDate = DateTime.UtcNow.AddDays(3),
            Purpose = "Thực hành IoT",
            Items = [new() { EquipmentId = 1 }, new() { EquipmentId = 2 }]
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("giảng viên bảo lãnh", badRequest.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(context.BorrowRecords);
    }

    [Fact]
    public async Task Teacher_can_create_one_request_with_multiple_details()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { Id = 2, Username = "teacher", Role = Roles.Teacher, IsActive = true });
        context.Equipments.AddRange(
            new Equipment { Id = 1, AssetCode = "IOT-001", QrToken = "qr-001", Name = "ESP32", Model = "ESP", Serial = "SN-001", Location = "Lab" },
            new Equipment { Id = 2, AssetCode = "IOT-002", QrToken = "qr-002", Name = "Gateway", Model = "GW", Serial = "SN-002", Location = "Lab" });
        await context.SaveChangesAsync();
        var controller = CreateBorrowController(context, 2, Roles.Teacher);

        var result = await controller.CreateRequest(new BorrowController.BorrowRequestDto
        {
            ExpectedReturnDate = DateTime.UtcNow.AddDays(3),
            Purpose = "Thực hành IoT",
            Items = [new() { EquipmentId = 1 }, new() { EquipmentId = 2 }]
        }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var record = await context.BorrowRecords.Include(item => item.Details).SingleAsync();
        Assert.Equal(2, record.Details.Count);
        Assert.Equal(BorrowStatuses.Pending, record.Status);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static MaintenanceScheduleController CreateController(AppDbContext context)
    {
        var controller = new MaintenanceScheduleController(context, new NoopAuditService(), new NoopNotificationService());
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "7"),
                new Claim(ClaimTypes.Role, Roles.Admin)
            ], "Test"))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private static BorrowController CreateBorrowController(AppDbContext context, int userId, string role)
    {
        var controller = new BorrowController(
            context,
            new NoopEmailService(),
            new NoopNotificationService(),
            new NoopAuditService(),
            new NoopFileStorage(),
            new ConfigurationBuilder().Build());
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, role)
            ], "Test"))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private sealed class NoopAuditService : IAuditService
    {
        public Task WriteAsync(HttpContext httpContext, string action, string entityType, object? entityId = null, object? details = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoopEmailService : IEmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoopNotificationService : INotificationService
    {
        public Task NotifyUserAsync(int userId, string type, string title, string message, string url, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task NotifyUsersAsync(IEnumerable<int> userIds, string type, string title, string message, string url, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task NotifyManagersAsync(string type, string title, string message, string url, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class NoopFileStorage : IFileStorage
    {
        public Task<StoredFile> SaveAsync(IFormFile file, string folder, IReadOnlySet<string> allowedExtensions, long maxBytes, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public bool IsSafePath(string path) => false;
        public string GetStorageKey(string storedPath) => storedPath;
        public Task<Stream?> OpenReadAsync(string path, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream?>(null);
        public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
