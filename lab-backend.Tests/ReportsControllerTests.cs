using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LabManagementAPI.Controllers;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Xunit;

namespace LabManagementAPI.Tests;

public sealed class ReportsControllerTests
{
    [Fact]
    public async Task Summary_total_is_not_truncated_by_the_one_hundred_row_preview()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.Users.Add(new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true });
        var equipment = Enumerable.Range(1, 105).Select(id => new Equipment
        {
            Id = id,
            AssetCode = $"EQ-{id:000}",
            QrToken = $"qr-{id:000}",
            Name = $"Thiết bị {id}",
            Serial = $"SN-{id:000}",
            Model = "M1",
            Location = "Lab",
            Status = EquipmentStatuses.Borrowed,
            CreatedAt = now.AddDays(-30)
        }).ToList();
        context.Equipments.AddRange(equipment);
        context.BorrowRecords.Add(new BorrowRecord
        {
            Id = 1,
            UserId = 1,
            BorrowDate = now.AddDays(-2),
            ExpectedReturnDate = now.AddDays(2),
            Purpose = "Kiểm thử",
            Status = BorrowStatuses.Borrowed,
            Details = equipment.Select(item => new BorrowRequestDetail
            {
                EquipmentId = item.Id,
                Status = BorrowStatuses.Borrowed
            }).ToList()
        });
        await context.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(await new ReportsController(context)
            .Summary(null, null, null, null, CancellationToken.None));
        var json = JsonSerializer.SerializeToElement(result.Value);

        Assert.Equal(105, json.GetProperty("totals").GetProperty("borrowed").GetInt32());
        Assert.Equal(100, json.GetProperty("borrowed").GetArrayLength());
    }

    [Fact]
    public async Task Summary_borrowed_total_includes_overdue_assets()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.Users.Add(new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true });
        context.Equipments.Add(new Equipment
        {
            Id = 1,
            AssetCode = "EQ-001",
            QrToken = "qr-001",
            Name = "Thiết bị quá hạn",
            Serial = "SN-001",
            Model = "M1",
            Location = "Lab",
            Status = EquipmentStatuses.Borrowed,
            CreatedAt = now
        });
        context.BorrowRecords.Add(new BorrowRecord
        {
            Id = 1,
            UserId = 1,
            ExpectedReturnDate = now.AddDays(-1),
            BorrowDate = now.AddDays(-3),
            Purpose = "Kiểm thử tổng đang mượn",
            Status = BorrowStatuses.Borrowed,
            Details = [new BorrowRequestDetail { EquipmentId = 1, Status = BorrowStatuses.Borrowed }]
        });
        await context.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(await new ReportsController(context)
            .Summary(null, null, null, null, CancellationToken.None));
        var totals = JsonSerializer.SerializeToElement(result.Value).GetProperty("totals");

        Assert.Equal(1, totals.GetProperty("borrowed").GetInt32());
        Assert.Equal(1, totals.GetProperty("overdue").GetInt32());
    }

    [Fact]
    public async Task Summary_reserved_equipment_includes_holder_identity_without_private_contact_data()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.Users.Add(new User
        {
            Id = 1,
            Username = "student01",
            FullName = "Nguyễn Văn A",
            UniversityCode = "SV001",
            Email = "student@example.com",
            Phone = "0900000000",
            Role = Roles.Student,
            IsActive = true
        });
        context.Equipments.Add(new Equipment
        {
            Id = 1,
            AssetCode = "EQ-001",
            QrToken = "qr-001",
            Name = "Thiết bị đang giữ chỗ",
            Serial = "SN-001",
            Model = "M1",
            Location = "Lab",
            Status = EquipmentStatuses.BorrowPending,
            CreatedAt = now
        });
        context.BorrowRecords.Add(new BorrowRecord
        {
            Id = 1,
            UserId = 1,
            BorrowDate = now.AddMinutes(-10),
            ExpectedReturnDate = now.AddDays(2),
            HoldExpiresAt = now.AddHours(12),
            Purpose = "Kiểm thử giữ chỗ",
            Status = BorrowStatuses.Approved,
            Details = [new BorrowRequestDetail { EquipmentId = 1, Status = BorrowStatuses.Approved }]
        });
        await context.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(await new ReportsController(context)
            .Summary(null, null, null, null, CancellationToken.None));
        var row = JsonSerializer.SerializeToElement(result.Value)
            .GetProperty("reservedEquipment")[0];

        Assert.Equal("Nguyễn Văn A", row.GetProperty("reservedByName").GetString());
        Assert.Equal("SV001", row.GetProperty("reservedByCode").GetString());
        Assert.False(row.TryGetProperty("email", out _));
        Assert.False(row.TryGetProperty("phone", out _));
    }

    [Fact]
    public async Task Summary_reserved_equipment_recovers_legacy_multi_asset_holder_and_hold_expiry()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.Users.Add(new User
        {
            Id = 1,
            Username = "student01",
            FullName = "Nguyễn Văn A",
            UniversityCode = "SV001",
            Role = Roles.Student,
            IsActive = true
        });
        context.Equipments.AddRange(
            new Equipment
            {
                Id = 1,
                AssetCode = "EQ-001",
                QrToken = "qr-001",
                Name = "Thiết bị giữ chỗ 1",
                Serial = "SN-001",
                Model = "M1",
                Location = "Lab",
                Status = EquipmentStatuses.BorrowPending,
                CreatedAt = now
            },
            new Equipment
            {
                Id = 2,
                AssetCode = "EQ-002",
                QrToken = "qr-002",
                Name = "Thiết bị giữ chỗ 2",
                Serial = "SN-002",
                Model = "M2",
                Location = "Lab",
                Status = EquipmentStatuses.BorrowPending,
                CreatedAt = now
            });
        context.BorrowRecords.Add(new BorrowRecord
        {
            Id = 1,
            UserId = 1,
            BorrowDate = now.AddHours(-1),
            ExpectedReturnDate = now.AddDays(2),
            Purpose = "Phiếu cũ nhiều tài sản",
            Status = BorrowStatuses.Approved,
            Details =
            [
                new BorrowRequestDetail { EquipmentId = 1, Status = BorrowStatuses.Pending },
                new BorrowRequestDetail { EquipmentId = 2, Status = BorrowStatuses.Pending }
            ]
        });
        await context.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(await new ReportsController(context)
            .Summary(null, null, null, null, CancellationToken.None));
        var rows = JsonSerializer.SerializeToElement(result.Value).GetProperty("reservedEquipment");

        Assert.Equal(2, rows.GetArrayLength());
        foreach (var row in rows.EnumerateArray())
        {
            Assert.Equal("Nguyễn Văn A", row.GetProperty("reservedByName").GetString());
            Assert.Equal("SV001", row.GetProperty("reservedByCode").GetString());
            Assert.NotEqual(JsonValueKind.Null, row.GetProperty("holdExpiresAt").ValueKind);
        }
    }

    [Fact]
    public async Task Summary_includes_overdue_return_processing_assets_in_borrowed_list()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.Users.Add(new User { Id = 1, Username = "student", Role = Roles.Student, IsActive = true });
        context.Equipments.Add(new Equipment
        {
            Id = 1,
            AssetCode = "EQ-001",
            QrToken = "qr-001",
            Name = "Thiết bị đang xử lý trả",
            Serial = "SN-001",
            Model = "M1",
            Location = "Lab",
            Status = EquipmentStatuses.Borrowed,
            CreatedAt = now
        });
        context.BorrowRecords.Add(new BorrowRecord
        {
            Id = 1,
            UserId = 1,
            ExpectedReturnDate = now.AddDays(-1),
            BorrowDate = now.AddDays(-3),
            Purpose = "Kiểm thử quá hạn đang xử lý trả",
            Status = BorrowStatuses.ReturnProcessing,
            Details = [new BorrowRequestDetail { EquipmentId = 1, Status = BorrowStatuses.ReturnProcessing }]
        });
        await context.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(await new ReportsController(context)
            .Summary(null, null, null, null, CancellationToken.None));
        var json = JsonSerializer.SerializeToElement(result.Value);

        Assert.Equal(1, json.GetProperty("totals").GetProperty("borrowed").GetInt32());
        Assert.Equal(1, json.GetProperty("totals").GetProperty("overdue").GetInt32());
        Assert.True(json.GetProperty("borrowed")[0].GetProperty("processingReturn").GetBoolean());
    }

    [Fact]
    public async Task Summary_borrowed_list_prefers_borrower_full_name_over_username()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.Users.Add(new User
        {
            Id = 1,
            Username = "sv01",
            FullName = "Nguyễn Văn B",
            Role = Roles.Student,
            IsActive = true
        });
        context.Equipments.Add(new Equipment
        {
            Id = 1,
            AssetCode = "EQ-001",
            QrToken = "qr-001",
            Name = "Thiết bị đang mượn",
            Serial = "SN-001",
            Model = "M1",
            Location = "Lab",
            Status = EquipmentStatuses.Borrowed,
            CreatedAt = now
        });
        context.BorrowRecords.Add(new BorrowRecord
        {
            Id = 1,
            UserId = 1,
            BorrowDate = now.AddDays(-1),
            ExpectedReturnDate = now.AddDays(1),
            Purpose = "Kiểm thử tên người mượn",
            Status = BorrowStatuses.Borrowed,
            Details = [new BorrowRequestDetail { EquipmentId = 1, Status = BorrowStatuses.Borrowed }]
        });
        await context.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(await new ReportsController(context)
            .Summary(null, null, null, null, CancellationToken.None));
        var row = JsonSerializer.SerializeToElement(result.Value)
            .GetProperty("borrowed")[0];

        Assert.Equal("Nguyễn Văn B", row.GetProperty("user").GetString());
    }

    [Fact]
    public async Task Summary_applies_equipment_scope_and_uses_available_consumable_stock()
    {
        await using var context = CreateContext();
        var day = new DateTime(2026, 9, 1, 18, 0, 0, DateTimeKind.Utc);
        context.AssetCategories.AddRange(
            new AssetCategory { Id = 1, Name = "IoT" },
            new AssetCategory { Id = 2, Name = "Đo lường" });
        context.Equipments.AddRange(
            new Equipment
            {
                Id = 1, AssetCode = "EQ-1", QrToken = "qr-1", Name = "ESP32", Serial = "SN-1",
                Model = "M", Location = "Lab", Status = EquipmentStatuses.MaintenanceInProgress,
                ResponsiblePerson = "Nguyễn Văn A",
                AssetCategoryId = 1, CreatedAt = day
            },
            new Equipment
            {
                Id = 2, AssetCode = "EQ-2", QrToken = "qr-2", Name = "Oscilloscope", Serial = "SN-2",
                Model = "M", Location = "Lab", Status = EquipmentStatuses.Available,
                AssetCategoryId = 2, CreatedAt = day
            });
        context.MaintenanceRecords.AddRange(
            new MaintenanceRecord { Id = 1, EquipmentId = 1, MaintenanceDate = day, Cost = 100, Status = MaintenanceStatuses.InProgress },
            new MaintenanceRecord { Id = 2, EquipmentId = 2, MaintenanceDate = day, Cost = 900, Status = MaintenanceStatuses.Completed });
        context.Consumables.Add(new Consumable
        {
            Id = 1,
            Code = "VT-1",
            Name = "Pin",
            Unit = "viên",
            Quantity = 10,
            ReservedQuantity = 8,
            MinQuantity = 5,
            AssetCategoryId = 1,
            CreatedAt = day
        });
        await context.SaveChangesAsync();

        var result = Assert.IsType<OkObjectResult>(await new ReportsController(context)
            .Summary(day.Date, day.Date, 1, null, CancellationToken.None));
        var json = JsonSerializer.SerializeToElement(result.Value);
        var totals = json.GetProperty("totals");

        Assert.Equal(1, totals.GetProperty("assets").GetInt32());
        Assert.Equal(100, totals.GetProperty("maintenanceCost").GetDecimal());
        Assert.Equal(1, totals.GetProperty("maintenanceInProgress").GetInt32());
        Assert.Equal(1, json.GetProperty("maintenance").GetArrayLength());
        Assert.Equal(1, json.GetProperty("lowStock").GetArrayLength());
        Assert.Equal(2, json.GetProperty("lowStock")[0].GetProperty("availableQuantity").GetInt32());
        Assert.Equal(1, json.GetProperty("consumables").GetArrayLength());
        Assert.Equal(1, json.GetProperty("responsible").GetArrayLength());
        Assert.Equal("Nguyễn Văn A", json.GetProperty("responsible")[0].GetProperty("responsiblePerson").GetString());
    }

    [Fact]
    public async Task Export_translates_equipment_and_maintenance_statuses_to_vietnamese()
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        context.Equipments.Add(new Equipment
        {
            Id = 1,
            AssetCode = "EQ-1",
            QrToken = "qr-1",
            Name = "Thiết bị lỗi",
            Serial = "SN-1",
            Model = "M",
            Location = "Lab",
            Status = EquipmentStatuses.Broken,
            CreatedAt = now
        });
        context.MaintenanceRecords.Add(new MaintenanceRecord
        {
            Id = 1,
            EquipmentId = 1,
            MaintenanceDate = now,
            Status = MaintenanceStatuses.Completed,
            Description = "Đã sửa",
            PerformedBy = "Kỹ thuật viên"
        });
        await context.SaveChangesAsync();

        var result = Assert.IsType<FileContentResult>(await new ReportsController(context)
            .Export(null, null, null, null, CancellationToken.None));
        using var stream = new MemoryStream(result.FileContents);
        using var package = new ExcelPackage(stream);

        Assert.Equal("Hỏng", package.Workbook.Worksheets["TaiSan"].Cells[2, 7].Text);
        Assert.Equal("Đã hoàn thành bảo trì", package.Workbook.Worksheets["BaoTri"].Cells[2, 6].Text);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
