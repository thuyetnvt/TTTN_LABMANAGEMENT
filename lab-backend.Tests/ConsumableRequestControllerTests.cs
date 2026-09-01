using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LabManagementAPI.Controllers;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LabManagementAPI.Tests;

public sealed class ConsumableRequestControllerTests
{
    [Fact]
    public async Task Approve_rejects_insufficient_stock_without_making_stock_negative()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            context.Users.AddRange(
                new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
                new User { Id = 99, Username = "manager", Role = Roles.LabHead, IsActive = true });
            context.Consumables.Add(new Consumable { Id = 1, Code = "VT-001", Name = "Điện trở", Unit = "cái", Quantity = 2 });
            context.ConsumableRequests.Add(new ConsumableRequest
            {
                Id = 10,
                ConsumableId = 1,
                UserId = 1,
                Quantity = 3,
                Reason = "Thực hành",
                Status = ConsumableRequestStatuses.Pending
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, 99, Roles.LabHead);
            var result = await controller.ApproveRequest(10, CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(result);
            var stock = await context.Consumables.AsNoTracking().SingleAsync();
            var request = await context.ConsumableRequests.AsNoTracking().SingleAsync();
            Assert.Equal(2, stock.Quantity);
            Assert.Equal(ConsumableRequestStatuses.Pending, request.Status);
            Assert.Empty(context.ConsumableTransactions);
        }
    }

    [Fact]
    public async Task Approve_reserves_stock_without_deducting_it_before_handover()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            context.Users.AddRange(
                new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
                new User { Id = 99, Username = "manager", Role = Roles.LabHead, IsActive = true });
            context.Consumables.Add(new Consumable { Id = 1, Code = "VT-001", Name = "Điện trở", Unit = "cái", Quantity = 5 });
            context.ConsumableRequests.Add(new ConsumableRequest
            {
                Id = 11,
                ConsumableId = 1,
                UserId = 1,
                Quantity = 2,
                Reason = "Thực hành",
                Status = ConsumableRequestStatuses.Pending
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, 99, Roles.LabHead);
            var result = await controller.ApproveRequest(11, CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            var stock = await context.Consumables.AsNoTracking().SingleAsync();
            var request = await context.ConsumableRequests.AsNoTracking().SingleAsync();
            Assert.Equal(5, stock.Quantity);
            Assert.Equal(2, stock.ReservedQuantity);
            Assert.Equal(ConsumableRequestStatuses.Approved, request.Status);
            Assert.Empty(await context.ConsumableTransactions.AsNoTracking().ToListAsync());
        }
    }

    [Fact]
    public async Task Handover_deducts_selected_lot_and_borrower_confirms_receipt()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            context.Users.AddRange(
                new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
                new User { Id = 99, Username = "manager", Role = Roles.LabHead, IsActive = true });
            context.Consumables.Add(new Consumable
            {
                Id = 1,
                Code = "VT-001",
                Name = "Điện trở",
                Unit = "cái",
                Quantity = 5
            });
            context.ConsumableLots.Add(new ConsumableLot
            {
                Id = 20,
                ConsumableId = 1,
                LotNumber = "LOT-001",
                InitialQuantity = 5,
                Quantity = 5,
                EntryDate = DateTime.UtcNow.AddDays(-1)
            });
            context.ConsumableRequests.Add(new ConsumableRequest
            {
                Id = 12,
                ConsumableId = 1,
                UserId = 1,
                Quantity = 2,
                Reason = "Thực hành",
                Status = ConsumableRequestStatuses.Pending
            });
            await context.SaveChangesAsync();

            var managerController = CreateController(context, 99, Roles.LabHead);
            Assert.IsType<OkObjectResult>(await managerController.ApproveRequest(12, CancellationToken.None));
            Assert.IsType<OkObjectResult>(await managerController.HandoverRequest(
                12,
                new ConsumableRequestController.HandoverConsumableDto
                {
                    Allocations =
                    [
                        new ConsumableRequestController.LotAllocationDto { LotId = 20, Quantity = 2 }
                    ]
                },
                CancellationToken.None));

            var stockAfterHandover = await context.Consumables.AsNoTracking().SingleAsync();
            var lotAfterHandover = await context.ConsumableLots.AsNoTracking().SingleAsync();
            var handoverRequest = await context.ConsumableRequests.AsNoTracking()
                .Include(item => item.LotAllocations)
                .SingleAsync();
            var stockTransaction = await context.ConsumableTransactions.AsNoTracking().SingleAsync();
            Assert.Equal(3, stockAfterHandover.Quantity);
            Assert.Equal(0, stockAfterHandover.ReservedQuantity);
            Assert.Equal(3, lotAfterHandover.Quantity);
            Assert.Equal(ConsumableRequestStatuses.HandedOver, handoverRequest.Status);
            Assert.Equal(2, handoverRequest.LotAllocations.Single().Quantity);
            Assert.Equal(12, stockTransaction.ConsumableRequestId);

            var borrowerController = CreateController(context, 1, Roles.Student);
            Assert.IsType<OkObjectResult>(await borrowerController.ConfirmReceipt(12, CancellationToken.None));
            var receivedRequest = await context.ConsumableRequests.AsNoTracking().SingleAsync();
            Assert.Equal(ConsumableRequestStatuses.Received, receivedRequest.Status);
            Assert.Equal(1, receivedRequest.ReceivedByUserId);
            Assert.NotNull(receivedRequest.ReceivedAt);
        }
    }

    [Fact]
    public async Task Confirm_receipt_rejects_a_different_borrower()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            context.Users.AddRange(
                new User { Id = 1, Username = "owner", Role = Roles.Student, IsActive = true },
                new User { Id = 2, Username = "other", Role = Roles.Student, IsActive = true });
            context.Consumables.Add(new Consumable { Id = 1, Code = "VT-001", Name = "Điện trở", Unit = "cái", Quantity = 3 });
            context.ConsumableRequests.Add(new ConsumableRequest
            {
                Id = 13,
                ConsumableId = 1,
                UserId = 1,
                Quantity = 1,
                Reason = "Thực hành",
                Status = ConsumableRequestStatuses.HandedOver
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, 2, Roles.Student);
            Assert.IsType<ConflictObjectResult>(await controller.ConfirmReceipt(13, CancellationToken.None));
            Assert.Equal(
                ConsumableRequestStatuses.HandedOver,
                (await context.ConsumableRequests.AsNoTracking().SingleAsync()).Status);
        }
    }

    private static AppDbContext CreateContext(out SqliteConnection connection)
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static ConsumableRequestController CreateController(AppDbContext context, int userId, string role)
    {
        var controller = new ConsumableRequestController(
            context,
            new NoopNotificationService(),
            new NoopAuditService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                ], "Test"))
            }
        };
        return controller;
    }

    private sealed class NoopAuditService : IAuditService
    {
        public Task WriteAsync(HttpContext httpContext, string action, string entityType, object? entityId = null, object? details = null, CancellationToken cancellationToken = default)
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
}
