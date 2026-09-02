using System;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using LabManagementAPI.Controllers;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace LabManagementAPI.Tests;

public sealed class DashboardControllerTests
{
    [Fact]
    public async Task Stats_supports_multi_asset_borrow_records_without_parent_equipment()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { Id = 1, Username = "admin", Role = Roles.Admin });
        context.Equipments.AddRange(
            new Equipment { Id = 1, AssetCode = "EQ-001", Serial = "SN-001", Name = "Kính hiển vi" },
            new Equipment { Id = 2, AssetCode = "EQ-002", Serial = "SN-002", Name = "Máy đo" });
        context.BorrowRecords.Add(new BorrowRecord
        {
            Id = 1,
            UserId = 1,
            EquipmentId = null,
            Status = BorrowStatuses.Pending,
            BorrowDate = DateTime.UtcNow,
            ExpectedReturnDate = DateTime.UtcNow.AddDays(7),
            Details =
            [
                new BorrowRequestDetail { EquipmentId = 1 },
                new BorrowRequestDetail { EquipmentId = 2 }
            ]
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.GetStats(CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        var updatedAt = response.Value?.GetType().GetProperty("UpdatedAt")?.GetValue(response.Value);
        Assert.IsType<DateTime>(updatedAt);
        var payload = JsonSerializer.Serialize(response.Value, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        Assert.Contains("Nhiều tài sản (2)", payload);
    }

    [Fact]
    public async Task Stats_separates_pending_borrow_and_consumable_alerts()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { Id = 1, Username = "admin", Role = Roles.Admin });
        context.Consumables.Add(new Consumable
        {
            Id = 1,
            Name = "Cảm biến siêu âm",
            Unit = "cái",
            Quantity = 8,
            MinQuantity = 10
        });
        context.ConsumableRequests.Add(new ConsumableRequest
        {
            Id = 1,
            ConsumableId = 1,
            UserId = 1,
            Quantity = 2,
            Reason = "Thực hành",
            Status = ConsumableRequestStatuses.Pending
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.GetStats(CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        var payload = JsonSerializer.Serialize(response.Value, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        Assert.Contains("pending-consumable-requests", payload);
        Assert.Contains("Yêu cầu cấp phát chờ duyệt", payload);
        Assert.DoesNotContain("pending-borrow-requests", payload);
    }

    [Fact]
    public async Task Stats_returns_action_counts_from_current_database_state()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.Users.Add(new User { Id = 1, Username = "admin", Role = Roles.Admin });
        context.Equipments.AddRange(
            new Equipment
            {
                Id = 1,
                AssetCode = "EQ-WARRANTY",
                Serial = "SN-WARRANTY",
                Name = "Thiết bị sắp hết bảo hành",
                Status = EquipmentStatuses.Available,
                WarrantyExpiry = now.AddDays(10)
            },
            new Equipment
            {
                Id = 2,
                AssetCode = "EQ-BORROWED",
                Serial = "SN-BORROWED",
                Name = "Thiết bị quá hạn",
                Status = EquipmentStatuses.Borrowed
            },
            new Equipment
            {
                Id = 3,
                AssetCode = "EQ-MAINTENANCE",
                Serial = "SN-MAINTENANCE",
                Name = "Thiết bị đang bảo trì",
                Status = EquipmentStatuses.MaintenanceInProgress
            });
        context.BorrowRecords.AddRange(
            new BorrowRecord
            {
                Id = 1,
                UserId = 1,
                Status = BorrowStatuses.Pending,
                BorrowDate = now.AddDays(-1),
                ExpectedReturnDate = now.AddDays(5)
            },
            new BorrowRecord
            {
                Id = 2,
                UserId = 1,
                EquipmentId = 2,
                Status = BorrowStatuses.Borrowed,
                BorrowDate = now.AddDays(-10),
                ExpectedReturnDate = now.AddDays(-2)
            });
        context.Consumables.Add(new Consumable
        {
            Id = 1,
            Name = "Vật tư sắp hết",
            Unit = "cái",
            Quantity = 2,
            MinQuantity = 5
        });
        context.ConsumableRequests.Add(new ConsumableRequest
        {
            Id = 1,
            ConsumableId = 1,
            UserId = 1,
            Quantity = 1,
            Reason = "Kiểm thử dashboard",
            Status = ConsumableRequestStatuses.Pending
        });
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetStats(CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, ReadInt(response.Value!, "PendingBorrowRequests"));
        Assert.Equal(1, ReadInt(response.Value!, "PendingConsumableRequests"));
        Assert.Equal(1, ReadInt(response.Value!, "OverdueBorrowRecords"));
        Assert.Equal(1, ReadInt(response.Value!, "LowStockConsumables"));
        Assert.Equal(1, ReadInt(response.Value!, "WarrantyExpiringSoon"));
        Assert.Equal(1, ReadInt(response.Value!, "MaintenanceInProgress"));
    }

    [Fact]
    public async Task Stats_returns_teacher_specific_work_instead_of_global_counts()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.Users.AddRange(
            new User { Id = 1, Username = "giangvien1", Role = Roles.Teacher },
            new User { Id = 2, Username = "sv1", Role = Roles.Student });
        context.Equipments.Add(new Equipment
        {
            Id = 1,
            AssetCode = "EQ-TEACHER",
            Serial = "SN-TEACHER",
            Name = "Bộ kit thực hành",
            Status = EquipmentStatuses.Borrowed
        });
        context.BorrowRecords.AddRange(
            new BorrowRecord
            {
                Id = 1,
                UserId = 2,
                TeacherId = 1,
                Status = BorrowStatuses.TeacherPending,
                BorrowDate = now,
                ExpectedReturnDate = now.AddDays(5)
            },
            new BorrowRecord
            {
                Id = 2,
                UserId = 1,
                Status = BorrowStatuses.Pending,
                BorrowDate = now.AddDays(-1),
                ExpectedReturnDate = now.AddDays(4)
            },
            new BorrowRecord
            {
                Id = 3,
                UserId = 1,
                EquipmentId = 1,
                Status = BorrowStatuses.Borrowed,
                BorrowDate = now.AddDays(-2),
                ExpectedReturnDate = now.AddDays(2)
            });
        await context.SaveChangesAsync();

        var result = await CreateController(context, Roles.Teacher).GetStats(CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        var summary = response.Value!.GetType().GetProperty("TeacherSummary")?.GetValue(response.Value);
        Assert.NotNull(summary);
        Assert.Equal(1, ReadInt(summary!, "PendingApprovals"));
        Assert.Equal(1, ReadInt(summary!, "PendingOwnRequests"));
        Assert.Equal(1, ReadInt(summary!, "ActiveBorrows"));
        Assert.Equal("Bộ kit thực hành", summary!.GetType().GetProperty("NextReturnEquipment")?.GetValue(summary));
    }

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.LabHead)]
    [InlineData(Roles.DeputyLabHead)]
    public async Task Stats_returns_manager_work_for_every_manager_role(string role)
    {
        await using var context = CreateContext();
        context.Users.Add(new User { Id = 1, Username = "manager", Role = role });
        context.BorrowRecords.Add(new BorrowRecord
        {
            Id = 1,
            UserId = 1,
            Status = BorrowStatuses.Pending,
            BorrowDate = DateTime.UtcNow,
            ExpectedReturnDate = DateTime.UtcNow.AddDays(2)
        });
        await context.SaveChangesAsync();

        var response = Assert.IsType<OkObjectResult>(
            await CreateController(context, role).GetStats(CancellationToken.None));

        Assert.Equal(1, ReadInt(response.Value!, "PendingBorrowRequests"));
        Assert.Equal(1, ReadInt(response.Value!, "BorrowRequestsToProcess"));
    }

    [Fact]
    public async Task Stats_limits_student_activity_and_overdue_counts_to_current_student()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.Users.AddRange(
            new User { Id = 1, Username = "sv1", Role = Roles.Student },
            new User { Id = 2, Username = "sv2", Role = Roles.Student });
        context.Equipments.AddRange(
            new Equipment { Id = 1, AssetCode = "EQ-STUDENT-1", Serial = "SN-STUDENT-1", Name = "Thiết bị của sv1", Status = EquipmentStatuses.Borrowed },
            new Equipment { Id = 2, AssetCode = "EQ-STUDENT-2", Serial = "SN-STUDENT-2", Name = "Thiết bị của sv2", Status = EquipmentStatuses.Borrowed });
        context.BorrowRecords.AddRange(
            new BorrowRecord
            {
                Id = 1,
                UserId = 1,
                EquipmentId = 1,
                Status = BorrowStatuses.Borrowed,
                BorrowDate = now.AddDays(-4),
                ExpectedReturnDate = now.AddDays(-1)
            },
            new BorrowRecord
            {
                Id = 2,
                UserId = 2,
                EquipmentId = 2,
                Status = BorrowStatuses.Borrowed,
                BorrowDate = now.AddDays(-5),
                ExpectedReturnDate = now.AddDays(-2)
            });
        await context.SaveChangesAsync();

        var response = Assert.IsType<OkObjectResult>(
            await CreateController(context, Roles.Student).GetStats(CancellationToken.None));
        var payload = JsonSerializer.Serialize(response.Value, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        Assert.Equal(1, ReadInt(response.Value!, "OverdueBorrowRecords"));
        Assert.Contains("Thiết bị của sv1", payload);
        Assert.DoesNotContain("Thiết bị của sv2", payload);

        var counts = response.Value!.GetType().GetProperty("Counts")!.GetValue(response.Value);
        Assert.Null(counts);

        var studentSummary = response.Value.GetType().GetProperty("StudentSummary")!.GetValue(response.Value)!;
        Assert.Equal(1, ReadInt(studentSummary, "ActiveBorrows"));
        Assert.Equal(0, ReadInt(studentSummary, "PendingRequests"));
        Assert.Equal(0, ReadInt(studentSummary, "ApprovedRequests"));
        Assert.Equal(0, ReadInt(studentSummary, "ReturnedBorrows"));
    }

    [Fact]
    public async Task Stats_caches_payload_per_dashboard_scope_for_short_reloads()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { Id = 1, Username = "admin", Role = Roles.Admin });
        context.Equipments.Add(new Equipment
        {
            Id = 1,
            AssetCode = "EQ-CACHE-1",
            Serial = "SN-CACHE-1",
            Name = "Thiết bị thứ nhất",
            Status = EquipmentStatuses.Available
        });
        await context.SaveChangesAsync();

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var first = Assert.IsType<OkObjectResult>(
            await CreateController(context, Roles.Admin, cache).GetStats(CancellationToken.None));

        context.Equipments.Add(new Equipment
        {
            Id = 2,
            AssetCode = "EQ-CACHE-2",
            Serial = "SN-CACHE-2",
            Name = "Thiết bị thứ hai",
            Status = EquipmentStatuses.Available
        });
        await context.SaveChangesAsync();

        var second = Assert.IsType<OkObjectResult>(
            await CreateController(context, Roles.Admin, cache).GetStats(CancellationToken.None));
        var firstCounts = first.Value!.GetType().GetProperty("Counts")!.GetValue(first.Value)!;
        var secondCounts = second.Value!.GetType().GetProperty("Counts")!.GetValue(second.Value)!;

        Assert.Equal(1, ReadInt(firstCounts, "Total"));
        Assert.Equal(1, ReadInt(secondCounts, "Total"));
        Assert.Same(first.Value, second.Value);

        var refreshed = Assert.IsType<OkObjectResult>(
            await CreateController(context, Roles.Admin, cache, forceRefresh: true)
                .GetStats(CancellationToken.None));
        var refreshedCounts = refreshed.Value!.GetType().GetProperty("Counts")!.GetValue(refreshed.Value)!;
        Assert.Equal(2, ReadInt(refreshedCounts, "Total"));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static DashboardController CreateController(
        AppDbContext context,
        string role = Roles.Admin,
        IMemoryCache? cache = null,
        bool forceRefresh = false)
    {
        var controller = new DashboardController(
            context,
            cache ?? new MemoryCache(new MemoryCacheOptions()));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, role)
                ], "Test"))
            }
        };
        if (forceRefresh)
        {
            controller.ControllerContext.HttpContext.Request.QueryString = new QueryString("?refresh=true");
        }
        return controller;
    }

    private static int ReadInt(object payload, string propertyName)
    {
        var property = payload.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return Assert.IsType<int>(property.GetValue(payload));
    }
}
