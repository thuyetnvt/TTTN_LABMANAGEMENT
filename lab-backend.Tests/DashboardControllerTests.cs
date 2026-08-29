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

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static DashboardController CreateController(AppDbContext context)
    {
        var controller = new DashboardController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, Roles.Admin)
                ], "Test"))
            }
        };
        return controller;
    }

    private static int ReadInt(object payload, string propertyName)
    {
        var property = payload.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return Assert.IsType<int>(property.GetValue(payload));
    }
}
