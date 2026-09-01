using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LabManagementAPI.Tests;

public sealed class HandoverControllerTests
{
    [Fact]
    public async Task Handover_stays_unconfirmed_until_borrower_confirms_receipt()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            await SeedApprovedBorrow(context);

            var manager = CreateController(context, 9, Roles.LabHead);
            var createResult = await manager.Create(new HandoverController.CreateHandoverDto
            {
                BorrowRecordId = 10,
                Notes = "Bàn giao tại phòng lab",
                Items =
                [
                    new HandoverController.HandoverItemDto
                    {
                        EquipmentId = 20,
                        Condition = EquipmentStatuses.Available,
                        Accessories = "Nguồn và cáp USB"
                    }
                ]
            }, CancellationToken.None);

            Assert.IsType<OkObjectResult>(createResult.Result);
            var draft = await context.HandoverRecords.AsNoTracking().SingleAsync();
            Assert.Null(draft.ConfirmedAt);
            Assert.Equal(BorrowStatuses.Approved, (await context.BorrowRecords.AsNoTracking().SingleAsync()).Status);
            Assert.Equal(EquipmentStatuses.BorrowPending, (await context.Equipments.AsNoTracking().SingleAsync()).Status);

            var borrower = CreateController(context, 1, Roles.Student);
            var confirmResult = await borrower.ConfirmReceipt(10, CancellationToken.None);

            Assert.IsType<OkObjectResult>(confirmResult);
            var record = await context.BorrowRecords.AsNoTracking().Include(item => item.Details).SingleAsync();
            var equipment = await context.Equipments.AsNoTracking().SingleAsync();
            var confirmedHandover = await context.HandoverRecords.AsNoTracking().SingleAsync();
            Assert.Equal(BorrowStatuses.Borrowed, record.Status);
            Assert.All(record.Details, item => Assert.Equal(BorrowStatuses.Borrowed, item.Status));
            Assert.Equal(EquipmentStatuses.Borrowed, equipment.Status);
            Assert.Equal(1, equipment.BorrowCount);
            Assert.NotNull(confirmedHandover.ConfirmedAt);
            Assert.Equal(1, confirmedHandover.ReceivedByUserId);
        }
    }

    [Fact]
    public async Task Another_borrower_cannot_read_or_confirm_handover()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            await SeedApprovedBorrow(context);
            context.Users.Add(new User { Id = 2, Username = "student-2", Role = Roles.Student, IsActive = true });
            context.HandoverRecords.Add(new HandoverRecord
            {
                Id = 30,
                Code = "BH-TEST-002",
                BorrowRecordId = 10,
                HandedOverByUserId = 9,
                ReceivedByUserId = 1,
                Items = [new HandoverItem { EquipmentId = 20, Condition = EquipmentStatuses.Available }]
            });
            await context.SaveChangesAsync();

            var otherBorrower = CreateController(context, 2, Roles.Student);

            Assert.IsType<ForbidResult>((await otherBorrower.Get(10, CancellationToken.None)).Result);
            Assert.IsType<ForbidResult>(await otherBorrower.ConfirmReceipt(10, CancellationToken.None));
        }
    }

    private static AppDbContext CreateContext(out SqliteConnection connection)
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task SeedApprovedBorrow(AppDbContext context)
    {
        context.Users.AddRange(
            new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
            new User { Id = 9, Username = "manager", Role = Roles.LabHead, IsActive = true });
        context.Equipments.Add(new Equipment
        {
            Id = 20,
            AssetCode = "IOT-020",
            QrToken = "qr-020",
            Name = "Gateway",
            Model = "GW",
            Serial = "SN-020",
            Location = "Lab",
            Status = EquipmentStatuses.BorrowPending
        });
        context.BorrowRecords.Add(new BorrowRecord
        {
            Id = 10,
            UserId = 1,
            Status = BorrowStatuses.Approved,
            BorrowDate = DateTime.UtcNow,
            ExpectedReturnDate = DateTime.UtcNow.AddDays(3),
            Purpose = "Kiểm thử bàn giao",
            Details =
            [
                new BorrowRequestDetail
                {
                    EquipmentId = 20,
                    Status = BorrowStatuses.Approved
                }
            ]
        });
        await context.SaveChangesAsync();
    }

    private static HandoverController CreateController(AppDbContext context, int userId, string role)
    {
        var controller = new HandoverController(
            context,
            new NoopAuditService(),
            new NoopNotificationService(),
            new NoopFileStorage(),
            new ConfigurationBuilder().Build());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                ], "Test"))
            }
        };
        return controller;
    }

    private sealed class NoopAuditService : IAuditService
    {
        public Task WriteAsync(HttpContext httpContext, string action, string entityType,
            object? entityId = null, object? details = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoopNotificationService : INotificationService
    {
        public Task NotifyUserAsync(int userId, string type, string title, string message,
            string url, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task NotifyUsersAsync(IEnumerable<int> userIds, string type, string title, string message,
            string url, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task NotifyManagersAsync(string type, string title, string message,
            string url, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoopFileStorage : IFileStorage
    {
        public Task<StoredFile> SaveAsync(IFormFile file, string folder,
            IReadOnlySet<string> allowedExtensions, long maxBytes,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public bool IsSafePath(string path) => false;
        public string GetStorageKey(string storedPath) => storedPath;
        public Task<Stream?> OpenReadAsync(string path,
            CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
        public Task DeleteAsync(string path,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
