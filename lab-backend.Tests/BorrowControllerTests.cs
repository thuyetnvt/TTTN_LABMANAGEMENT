using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
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

public sealed class BorrowControllerTests
{
    [Fact]
    public async Task Student_request_with_teacher_is_teacher_pending_and_preserves_all_items()
    {
        await using var context = CreateInMemoryContext();
        context.Users.AddRange(
            new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
            new User { Id = 2, Username = "teacher", Role = Roles.Teacher, IsActive = true });
        context.Equipments.AddRange(CreateEquipment(1), CreateEquipment(2));
        await context.SaveChangesAsync();

        var controller = CreateController(context, 1, Roles.Student);
        var result = await controller.CreateRequest(new BorrowController.BorrowRequestDto
        {
            TeacherId = 2,
            ExpectedReturnDate = DateTime.UtcNow.AddDays(3),
            Purpose = "Thực hành mạng IoT",
            Items = [new() { EquipmentId = 1, Note = "Thiết bị chính" }, new() { EquipmentId = 2 }]
        }, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var record = await context.BorrowRecords.Include(item => item.Details).SingleAsync();
        Assert.Equal(BorrowStatuses.TeacherPending, record.Status);
        Assert.Equal(2, record.Details.Count);
        Assert.All(record.Details, detail => Assert.Equal(BorrowStatuses.TeacherPending, detail.Status));
    }

    [Fact]
    public async Task Teacher_cannot_approve_request_assigned_to_another_teacher()
    {
        await using var context = CreateSqliteContext(out var connection);
        await using (connection)
        {
            context.Users.AddRange(
                new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
                new User { Id = 2, Username = "teacher-a", Role = Roles.Teacher, IsActive = true },
                new User { Id = 3, Username = "teacher-b", Role = Roles.Teacher, IsActive = true });
            context.Equipments.Add(CreateEquipment(1));
            context.BorrowRecords.Add(new BorrowRecord
            {
                Id = 10,
                UserId = 1,
                TeacherId = 2,
                Status = BorrowStatuses.TeacherPending,
                BorrowDate = DateTime.UtcNow,
                ExpectedReturnDate = DateTime.UtcNow.AddDays(2),
                Purpose = "Kiểm tra quyền",
                Details = [new BorrowRequestDetail { EquipmentId = 1, Status = BorrowStatuses.TeacherPending }]
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, 3, Roles.Teacher);
            var result = await controller.TeacherApproveRequest(
                10,
                new BorrowController.DecisionNoteDto { Note = "Đồng ý" },
                CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(BorrowStatuses.TeacherPending, (await context.BorrowRecords.AsNoTracking().SingleAsync()).Status);
        }
    }

    [Fact]
    public async Task Manager_approval_reserves_all_items_until_handover_is_confirmed()
    {
        await using var context = CreateSqliteContext(out var connection);
        await using (connection)
        {
            context.Users.AddRange(
                new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
                new User { Id = 99, Username = "manager", Role = Roles.LabHead, IsActive = true });
            context.Equipments.AddRange(CreateEquipment(1), CreateEquipment(2));
            context.BorrowRecords.Add(CreateBorrowRecord(20, 1, BorrowStatuses.Pending, 1, 2));
            await context.SaveChangesAsync();

            var controller = CreateController(context, 99, Roles.LabHead);
            var result = await controller.ApproveRequest(20, CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            var record = await context.BorrowRecords.AsNoTracking().Include(item => item.Details).SingleAsync(item => item.Id == 20);
            Assert.Equal(BorrowStatuses.Approved, record.Status);
            Assert.All(record.Details, detail => Assert.Equal(BorrowStatuses.Approved, detail.Status));
            Assert.All(await context.Equipments.AsNoTracking().ToListAsync(), item =>
            {
                Assert.Equal(EquipmentStatuses.BorrowPending, item.Status);
                Assert.Equal(0, item.BorrowCount);
            });
        }
    }

    [Fact]
    public async Task Manager_approval_rolls_back_when_one_item_is_no_longer_available()
    {
        await using var context = CreateSqliteContext(out var connection);
        await using (connection)
        {
            context.Users.Add(new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true });
            context.Equipments.AddRange(
                CreateEquipment(1),
                CreateEquipment(2, EquipmentStatuses.Borrowed));
            context.BorrowRecords.Add(CreateBorrowRecord(21, 1, BorrowStatuses.Pending, 1, 2));
            await context.SaveChangesAsync();

            var controller = CreateController(context, 99, Roles.LabHead);
            var result = await controller.ApproveRequest(21, CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(result);
            var record = await context.BorrowRecords.AsNoTracking().SingleAsync(item => item.Id == 21);
            var equipment = await context.Equipments.AsNoTracking().OrderBy(item => item.Id).ToListAsync();
            Assert.Equal(BorrowStatuses.Pending, record.Status);
            Assert.Equal(EquipmentStatuses.Available, equipment[0].Status);
            Assert.Equal(EquipmentStatuses.Borrowed, equipment[1].Status);
        }
    }

    [Fact]
    public async Task Student_history_does_not_return_another_users_record()
    {
        await using var context = CreateInMemoryContext();
        context.Users.AddRange(
            new User { Id = 1, Username = "student-a", Role = Roles.Student, IsActive = true },
            new User { Id = 2, Username = "student-b", Role = Roles.Student, IsActive = true });
        context.Equipments.AddRange(CreateEquipment(1), CreateEquipment(2));
        context.BorrowRecords.AddRange(
            CreateBorrowRecord(30, 1, BorrowStatuses.Returned, 1),
            CreateBorrowRecord(31, 2, BorrowStatuses.Returned, 2));
        await context.SaveChangesAsync();

        var controller = CreateController(context, 1, Roles.Student);
        var result = await controller.GetHistory(CancellationToken.None);
        var json = JsonSerializer.Serialize(Assert.IsType<OkObjectResult>(result.Result).Value);
        using var document = JsonDocument.Parse(json);
        var ids = document.RootElement.EnumerateArray().Select(item => item.GetProperty("id").GetInt32()).ToArray();

        Assert.Equal([30], ids);
    }

    [Fact]
    public async Task Returning_damaged_out_of_warranty_item_creates_penalty_and_maintenance()
    {
        await using var context = CreateSqliteContext(out var connection);
        await using (connection)
        {
            context.Users.AddRange(
                new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
                new User { Id = 99, Username = "manager", Role = Roles.LabHead, IsActive = true });
            context.Equipments.Add(new Equipment
            {
                Id = 1,
                AssetCode = "IOT-001",
                QrToken = "qr-001",
                Name = "Gateway",
                Model = "GW",
                Serial = "SN-001",
                Location = "Lab",
                Status = EquipmentStatuses.Borrowed,
                WarrantyExpiry = DateTime.UtcNow.AddDays(-1)
            });
            context.BorrowRecords.Add(CreateBorrowRecord(40, 1, BorrowStatuses.Borrowed, 1));
            await context.SaveChangesAsync();

            var controller = CreateController(context, 99, Roles.LabHead);
            var result = await controller.ReturnEquipment(
                40,
                new BorrowController.ReturnInspectionDto
                {
                    Items = [new()
                    {
                        EquipmentId = 1,
                        Condition = EquipmentStatuses.Broken,
                        Note = "Mất tín hiệu",
                        CompensationAmount = 350000
                    }]
                },
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            var record = await context.BorrowRecords.AsNoTracking().SingleAsync();
            var equipment = await context.Equipments.AsNoTracking().SingleAsync();
            Assert.Equal(BorrowStatuses.ReturnedDamaged, record.Status);
            Assert.Equal(EquipmentStatuses.Broken, equipment.Status);
            Assert.Equal(350000, record.CompensationAmount);
            Assert.Single(context.Penalties);
            Assert.Single(context.MaintenanceRecords);
        }
    }

    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static AppDbContext CreateSqliteContext(out SqliteConnection connection)
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

    private static Equipment CreateEquipment(int id, string status = EquipmentStatuses.Available) => new()
    {
        Id = id,
        AssetCode = $"IOT-{id:000}",
        QrToken = $"qr-{id:000}",
        Name = $"Thiết bị {id}",
        Model = "Model",
        Serial = $"SN-{id:000}",
        Location = "Phòng Lab",
        Status = status
    };

    private static BorrowRecord CreateBorrowRecord(int id, int userId, string status, params int[] equipmentIds) => new()
    {
        Id = id,
        UserId = userId,
        BorrowDate = DateTime.UtcNow,
        ExpectedReturnDate = DateTime.UtcNow.AddDays(3),
        Purpose = "Kiểm thử nghiệp vụ",
        Status = status,
        Details = equipmentIds.Select(equipmentId => new BorrowRequestDetail
        {
            EquipmentId = equipmentId,
            Quantity = 1,
            Note = "Kiểm thử",
            Status = status
        }).ToList()
    };

    private static BorrowController CreateController(AppDbContext context, int userId, string role)
    {
        var controller = new BorrowController(
            context,
            new NoopEmailService(),
            new NoopNotificationService(),
            new NoopAuditService(),
            new NoopFileStorage(),
            new ConfigurationBuilder().Build());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role),
                    new Claim(ClaimTypes.Name, role)
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
