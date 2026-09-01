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
