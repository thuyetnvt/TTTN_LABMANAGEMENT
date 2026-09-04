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
            Assert.True(record.HoldExpiresAt > DateTime.UtcNow);
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
    public async Task Borrower_can_cancel_pending_request_and_reason_is_recorded()
    {
        await using var context = CreateSqliteContext(out var connection);
        await using (connection)
        {
            context.Users.AddRange(
                new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
                new User { Id = 2, Username = "teacher", Role = Roles.Teacher, IsActive = true });
            context.Equipments.Add(CreateEquipment(1));
            context.BorrowRecords.Add(new BorrowRecord
            {
                Id = 22,
                UserId = 1,
                TeacherId = 2,
                BorrowDate = DateTime.UtcNow,
                ExpectedReturnDate = DateTime.UtcNow.AddDays(3),
                Purpose = "Hủy yêu cầu thử nghiệm",
                Status = BorrowStatuses.TeacherPending,
                Details = [new BorrowRequestDetail { EquipmentId = 1, Quantity = 1, Status = BorrowStatuses.TeacherPending }]
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, 1, Roles.Student);
            var result = await controller.CancelRequest(
                22,
                new BorrowController.CancelBorrowRequestDto { Reason = "Không còn nhu cầu sử dụng." },
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            var record = await context.BorrowRecords.AsNoTracking().Include(item => item.Details).SingleAsync();
            Assert.Equal(BorrowStatuses.Cancelled, record.Status);
            Assert.Equal("Không còn nhu cầu sử dụng.", record.CancellationReason);
            Assert.NotNull(record.CancelledAt);
            Assert.Equal(BorrowStatuses.Cancelled, record.Details.Single().Status);
        }
    }

    [Fact]
    public async Task Manager_can_cancel_approved_request_and_release_reserved_equipment()
    {
        await using var context = CreateSqliteContext(out var connection);
        await using (connection)
        {
            context.Users.AddRange(
                new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
                new User { Id = 99, Username = "manager", Role = Roles.LabHead, IsActive = true });
            context.Equipments.Add(CreateEquipment(1, EquipmentStatuses.BorrowPending));
            context.BorrowRecords.Add(new BorrowRecord
            {
                Id = 23,
                UserId = 1,
                BorrowDate = DateTime.UtcNow,
                ExpectedReturnDate = DateTime.UtcNow.AddDays(3),
                HoldExpiresAt = DateTime.UtcNow.AddHours(12),
                Purpose = "Hủy phiếu đã duyệt",
                Status = BorrowStatuses.Approved,
                Details = [new BorrowRequestDetail { EquipmentId = 1, Quantity = 1, Status = BorrowStatuses.Approved }]
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, 99, Roles.LabHead);
            var result = await controller.CancelRequest(
                23,
                new BorrowController.CancelBorrowRequestDto { Reason = "Thiết bị cần ưu tiên cho buổi học khác." },
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            var record = await context.BorrowRecords.AsNoTracking().Include(item => item.Details).SingleAsync();
            Assert.Equal(BorrowStatuses.Cancelled, record.Status);
            Assert.Null(record.HoldExpiresAt);
            Assert.Equal(BorrowStatuses.Cancelled, record.Details.Single().Status);
            Assert.Equal(EquipmentStatuses.Available, (await context.Equipments.AsNoTracking().SingleAsync()).Status);
            Assert.Contains(await context.BorrowStatusHistories.AsNoTracking().ToListAsync(), history =>
                history.FromStatus == BorrowStatuses.Approved && history.ToStatus == BorrowStatuses.Cancelled);
        }
    }

    [Fact]
    public async Task Manager_cannot_cancel_after_handover_exists()
    {
        await using var context = CreateSqliteContext(out var connection);
        await using (connection)
        {
            context.Users.AddRange(
                new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
                new User { Id = 99, Username = "manager", Role = Roles.LabHead, IsActive = true });
            context.Equipments.Add(CreateEquipment(1, EquipmentStatuses.BorrowPending));
            context.BorrowRecords.Add(new BorrowRecord
            {
                Id = 24,
                UserId = 1,
                BorrowDate = DateTime.UtcNow,
                ExpectedReturnDate = DateTime.UtcNow.AddDays(3),
                Status = BorrowStatuses.Approved,
                Details = [new BorrowRequestDetail { EquipmentId = 1, Quantity = 1, Status = BorrowStatuses.Approved }]
            });
            context.HandoverRecords.Add(new HandoverRecord
            {
                Id = 1,
                Code = "BH-TEST-24",
                BorrowRecordId = 24,
                HandedOverByUserId = 99,
                ReceivedByUserId = 1,
                Items = [new HandoverItem { EquipmentId = 1, Condition = EquipmentStatuses.Available }]
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context, 99, Roles.LabHead);
            var result = await controller.CancelRequest(
                24,
                new BorrowController.CancelBorrowRequestDto { Reason = "Không được phép sau bàn giao." },
                CancellationToken.None);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(BorrowStatuses.Approved, (await context.BorrowRecords.AsNoTracking().SingleAsync()).Status);
            Assert.Equal(EquipmentStatuses.BorrowPending, (await context.Equipments.AsNoTracking().SingleAsync()).Status);
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
    public async Task Student_history_returns_teacher_guarantor_without_private_data()
    {
        await using var context = CreateInMemoryContext();
        context.Users.AddRange(
            new User
            {
                Id = 1,
                Username = "student",
                FullName = "Nguyễn Văn A",
                Role = Roles.Student,
                IsActive = true
            },
            new User
            {
                Id = 2,
                Username = "teacher-a",
                FullName = "Trần Thị B",
                UniversityCode = "GV001",
                Email = "teacher@example.com",
                Phone = "0900000000",
                Role = Roles.Teacher,
                IsActive = true
            });
        context.Equipments.Add(CreateEquipment(1));
        var record = CreateBorrowRecord(33, 1, BorrowStatuses.Returned, 1);
        record.TeacherId = 2;
        record.TeacherDecisionNote = "Đồng ý bảo lãnh.";
        context.BorrowRecords.Add(record);
        await context.SaveChangesAsync();

        var controller = CreateController(context, 1, Roles.Student);
        var historyResult = await controller.GetHistory(CancellationToken.None);
        using var historyJson = JsonDocument.Parse(JsonSerializer.Serialize(
            Assert.IsType<OkObjectResult>(historyResult.Result).Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var historyItem = historyJson.RootElement[0];

        Assert.Equal(2, historyItem.GetProperty("teacherId").GetInt32());
        Assert.Equal("Trần Thị B", historyItem.GetProperty("teacherName").GetString());
        Assert.Equal("GV001", historyItem.GetProperty("teacherCode").GetString());
        Assert.Equal("Đồng ý bảo lãnh.", historyItem.GetProperty("teacherDecisionNote").GetString());
        Assert.False(historyItem.TryGetProperty("email", out _));
        Assert.False(historyItem.TryGetProperty("phone", out _));

        var pagedResult = await controller.GetHistoryPaged(
            new LabManagementAPI.Dtos.PageQuery { PageSize = 20 },
            CancellationToken.None);
        using var pagedJson = JsonDocument.Parse(JsonSerializer.Serialize(
            Assert.IsType<OkObjectResult>(pagedResult).Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var pagedItem = pagedJson.RootElement.GetProperty("items")[0];

        Assert.Equal("Trần Thị B", pagedItem.GetProperty("teacherName").GetString());
        Assert.False(pagedItem.TryGetProperty("email", out _));
        Assert.False(pagedItem.TryGetProperty("phone", out _));
    }

    [Fact]
    public async Task Student_history_falls_back_to_teacher_username_when_full_name_is_missing()
    {
        await using var context = CreateInMemoryContext();
        context.Users.AddRange(
            new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
            new User { Id = 2, Username = "teacher-a", FullName = string.Empty, Role = Roles.Teacher, IsActive = true });
        context.Equipments.Add(CreateEquipment(1));
        var record = CreateBorrowRecord(34, 1, BorrowStatuses.Returned, 1);
        record.TeacherId = 2;
        context.BorrowRecords.Add(record);
        await context.SaveChangesAsync();

        var controller = CreateController(context, 1, Roles.Student);
        var result = await controller.GetHistoryPaged(
            new LabManagementAPI.Dtos.PageQuery { PageSize = 20 },
            CancellationToken.None);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(
            Assert.IsType<OkObjectResult>(result).Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        Assert.Equal("teacher-a", json.RootElement.GetProperty("items")[0].GetProperty("teacherName").GetString());
    }

    [Fact]
    public async Task Student_paged_history_marks_overdue_with_number_of_days()
    {
        await using var context = CreateInMemoryContext();
        var today = VietnamTime.Today();
        context.Users.Add(new User
        {
            Id = 1,
            Username = "student",
            FullName = "Nguyễn Văn A",
            Role = Roles.Student,
            IsActive = true
        });
        context.Equipments.Add(CreateEquipment(1, EquipmentStatuses.Borrowed));
        context.BorrowRecords.Add(new BorrowRecord
        {
            Id = 32,
            UserId = 1,
            EquipmentId = 1,
            BorrowDate = DateTime.UtcNow.AddDays(-5),
            ExpectedReturnDate = VietnamTime.StartOfDayUtc(today.AddDays(-2)),
            Purpose = "Kiểm tra quá hạn",
            Status = BorrowStatuses.Borrowed
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, 1, Roles.Student);
        var result = await controller.GetHistoryPaged(
            new LabManagementAPI.Dtos.PageQuery(),
            CancellationToken.None);
        var json = JsonSerializer.Serialize(
            Assert.IsType<OkObjectResult>(result).Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        var item = document.RootElement.GetProperty("items")[0];

        Assert.Equal("Nguyễn Văn A", item.GetProperty("borrowerName").GetString());
        Assert.True(item.GetProperty("isOverdue").GetBoolean());
        Assert.Equal(-2, item.GetProperty("daysUntilDue").GetInt32());
        Assert.Equal(20000, item.GetProperty("overduePenaltyAmount").GetDecimal());
    }

    [Fact]
    public async Task Manager_pending_queue_excludes_borrowed_and_return_processing_records()
    {
        await using var context = CreateInMemoryContext();
        context.Users.Add(new User
        {
            Id = 1,
            Username = "student",
            FullName = "Nguyễn Văn A",
            Role = Roles.Student,
            IsActive = true
        });
        context.Equipments.AddRange(
            CreateEquipment(1),
            CreateEquipment(2),
            CreateEquipment(3, EquipmentStatuses.Borrowed),
            CreateEquipment(4, EquipmentStatuses.Borrowed));
        context.BorrowRecords.AddRange(
            CreateBorrowRecord(60, 1, BorrowStatuses.Pending, 1),
            CreateBorrowRecord(61, 1, BorrowStatuses.Approved, 2),
            CreateBorrowRecord(62, 1, BorrowStatuses.Borrowed, 3),
            CreateBorrowRecord(63, 1, BorrowStatuses.ReturnProcessing, 4));
        await context.SaveChangesAsync();

        var controller = CreateController(context, 99, Roles.LabHead);
        var result = await controller.GetPendingRequestsPaged(
            new LabManagementAPI.Dtos.PageQuery { PageSize = 100 },
            CancellationToken.None);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(
            Assert.IsType<OkObjectResult>(result).Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var ids = json.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetInt32())
            .ToArray();

        Assert.Equal([61, 60], ids);
    }

    [Fact]
    public async Task Manager_pending_queue_hides_internal_seed_marker_from_purpose()
    {
        await using var context = CreateInMemoryContext();
        context.Users.Add(new User
        {
            Id = 1,
            Username = "student",
            FullName = "Nguyễn Văn A",
            Role = Roles.Student,
            IsActive = true
        });
        context.Equipments.Add(CreateEquipment(1));
        var record = CreateBorrowRecord(66, 1, BorrowStatuses.Pending, 1);
        record.Purpose = "[SEED-FULL-BORROW-003] Huấn luyện mô hình nhận diện ảnh.";
        context.BorrowRecords.Add(record);
        await context.SaveChangesAsync();

        var controller = CreateController(context, 99, Roles.LabHead);
        var result = await controller.GetPendingRequestsPaged(
            new LabManagementAPI.Dtos.PageQuery { PageSize = 100 },
            CancellationToken.None);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(
            Assert.IsType<OkObjectResult>(result).Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        Assert.Equal(
            "Huấn luyện mô hình nhận diện ảnh.",
            json.RootElement.GetProperty("items")[0].GetProperty("purpose").GetString());
    }

    [Fact]
    public async Task Manager_history_contains_borrowed_records_but_not_approved_records()
    {
        await using var context = CreateInMemoryContext();
        context.Users.Add(new User
        {
            Id = 1,
            Username = "student",
            FullName = "Nguyễn Văn A",
            Role = Roles.Student,
            IsActive = true
        });
        context.Equipments.AddRange(
            CreateEquipment(5, EquipmentStatuses.BorrowPending),
            CreateEquipment(6, EquipmentStatuses.Borrowed));
        context.BorrowRecords.AddRange(
            CreateBorrowRecord(64, 1, BorrowStatuses.Approved, 5),
            CreateBorrowRecord(65, 1, BorrowStatuses.Borrowed, 6));
        await context.SaveChangesAsync();

        var controller = CreateController(context, 99, Roles.LabHead);
        var result = await controller.GetHistoryPaged(
            new LabManagementAPI.Dtos.PageQuery { PageSize = 100 },
            CancellationToken.None);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(
            Assert.IsType<OkObjectResult>(result).Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var ids = json.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetInt32())
            .ToArray();

        Assert.Equal([65], ids);
    }

    [Fact]
    public async Task Manager_can_create_in_app_return_reminder_without_smtp()
    {
        await using var context = CreateInMemoryContext();
        context.Users.Add(new User
        {
            Id = 1,
            Username = "student",
            Email = "student@lab.local",
            Role = Roles.Student,
            IsActive = true
        });
        context.Equipments.Add(CreateEquipment(1, EquipmentStatuses.Borrowed));
        context.BorrowRecords.Add(CreateBorrowRecord(50, 1, "BORROWED ", 1));
        await context.SaveChangesAsync();

        var controller = CreateController(context, 99, Roles.LabHead, new ThrowingEmailService());
        var result = await controller.RemindReturn(50, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response.Value));
        Assert.False(json.RootElement.GetProperty("emailSent").GetBoolean());
        Assert.Contains("SMTP", json.RootElement.GetProperty("message").GetString());
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

    [Fact]
    public async Task Returning_overdue_record_creates_paid_overdue_penalty()
    {
        await using var context = CreateSqliteContext(out var connection);
        await using (connection)
        {
            context.Users.AddRange(
                new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true },
                new User { Id = 99, Username = "manager", Role = Roles.LabHead, IsActive = true });
            context.Equipments.Add(CreateEquipment(1, EquipmentStatuses.Borrowed));
            var record = CreateBorrowRecord(41, 1, BorrowStatuses.Borrowed, 1);
            record.ExpectedReturnDate = VietnamTime.StartOfDayUtc(VietnamTime.Today().AddDays(-2));
            context.BorrowRecords.Add(record);
            await context.SaveChangesAsync();

            var controller = CreateController(context, 99, Roles.LabHead);
            var result = await controller.ReturnEquipment(
                41,
                new BorrowController.ReturnInspectionDto
                {
                    Items = [new()
                    {
                        EquipmentId = 1,
                        Condition = EquipmentStatuses.Available
                    }]
                },
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            var penalty = await context.Penalties.AsNoTracking().SingleAsync();
            Assert.Equal(20000m, penalty.Amount);
            Assert.Equal(PenaltyStatuses.Paid, penalty.Status);
            Assert.NotNull(penalty.PaidAt);
            Assert.StartsWith("Tự động phạt trả quá hạn", penalty.Reason);
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

    private static BorrowController CreateController(
        AppDbContext context,
        int userId,
        string role,
        IEmailService? emailService = null,
        IConfiguration? configuration = null)
    {
        var controller = new BorrowController(
            context,
            emailService ?? new NoopEmailService(),
            new NoopNotificationService(),
            new NoopAuditService(),
            new NoopFileStorage(),
            configuration ?? new ConfigurationBuilder().Build());
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

    private sealed class ThrowingEmailService : IEmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("SMTP is unavailable.");
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
