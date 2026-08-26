using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LabManagementAPI.Controllers;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Xunit;

namespace LabManagementAPI.Tests;

public sealed class ReportsControllerTests
{
    [Fact]
    public async Task Export_returns_all_named_sheets_and_empty_rows_without_data()
    {
        await using var context = CreateContext();
        var result = await CreateController(context).Export(null, null, null, null, null, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        Assert.NotEmpty(file.FileContents);
        Assert.Contains("Báo cáo tài sản Phòng Lab IoT", file.FileDownloadName, StringComparison.Ordinal);

        using var package = new ExcelPackage(new MemoryStream(file.FileContents));
        Assert.Equal(["Tài sản", "Bảo trì", "Đang mượn", "Vật tư"],
            package.Workbook.Worksheets.Select(sheet => sheet.Name));
        foreach (var sheet in package.Workbook.Worksheets)
        {
            Assert.Equal("Không có dữ liệu", sheet.Cells[2, 1].Text);
        }
    }

    [Fact]
    public async Task Export_applies_date_category_and_location_filters()
    {
        await using var context = CreateContext();
        context.AssetCategories.Add(new AssetCategory { Id = 10, Name = "Thiết bị đo" });
        context.LocationNodes.Add(new LocationNode { Id = 20, Code = "LAB-A", Name = "Phòng Lab A" });
        context.Equipments.AddRange(
            new Equipment
            {
                Id = 1,
                AssetCode = "IN-RANGE",
                Name = "Thiết bị trong phạm vi",
                AssetCategoryId = 10,
                LocationNodeId = 20,
                CreatedAt = new DateTime(2026, 8, 10, 3, 0, 0, DateTimeKind.Utc)
            },
            new Equipment
            {
                Id = 2,
                AssetCode = "OUT-RANGE",
                Name = "Thiết bị ngoài phạm vi",
                AssetCategoryId = 10,
                LocationNodeId = 20,
                CreatedAt = new DateTime(2026, 8, 11, 3, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        var result = await CreateController(context).Export(
            new DateTime(2026, 8, 10),
            new DateTime(2026, 8, 10),
            10,
            20,
            null,
            CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        using var package = new ExcelPackage(new MemoryStream(file.FileContents));
        var assets = package.Workbook.Worksheets["Tài sản"];
        Assert.Equal("IN-RANGE", assets.Cells[2, 1].Text);
        Assert.Equal(string.Empty, assets.Cells[3, 1].Text);
    }

    [Fact]
    public async Task Export_handles_null_relations_and_values()
    {
        await using var context = CreateContext();
        context.Equipments.Add(new Equipment
        {
            Id = 1,
            AssetCode = "NULL-RELATION",
            Name = string.Empty,
            Model = string.Empty,
            Serial = string.Empty,
            Location = string.Empty,
            AssetCategory = null,
            LocationNode = null,
            WarrantyExpiry = null
        });
        context.MaintenanceRecords.Add(new MaintenanceRecord
        {
            Id = 1,
            EquipmentId = 1,
            Equipment = null,
            Description = string.Empty,
            PerformedBy = string.Empty,
            Result = string.Empty,
            Cost = 0
        });
        context.Consumables.Add(new Consumable { Id = 1, Name = string.Empty, Unit = string.Empty, Quantity = 0, MinQuantity = 1 });
        await context.SaveChangesAsync();

        var result = await CreateController(context).Export(null, null, null, null, null, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.NotEmpty(file.FileContents);
        using var package = new ExcelPackage(new MemoryStream(file.FileContents));
        Assert.Equal("Không có dữ liệu", package.Workbook.Worksheets["Đang mượn"].Cells[2, 1].Text);
    }

    [Fact]
    public async Task ExportPdf_returns_readable_pdf_when_data_is_empty()
    {
        await using var context = CreateContext();
        var result = await CreateController(context).ExportPdf(null, null, null, null, null, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.NotEmpty(file.FileContents);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(file.FileContents, 0, 4));
        Assert.Contains("Báo cáo tài sản Phòng Lab IoT", file.FileDownloadName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportPdf_handles_long_text_and_missing_related_records()
    {
        await using var context = CreateContext();
        context.Equipments.Add(new Equipment
        {
            Id = 1,
            AssetCode = "PDF-001",
            Name = new string('A', 1000),
            Serial = string.Empty,
            Location = string.Empty,
            Status = string.Empty
        });
        context.MaintenanceRecords.Add(new MaintenanceRecord
        {
            Id = 1,
            EquipmentId = 99,
            Equipment = null,
            Description = new string('B', 2000),
            Cost = 125000
        });
        await context.SaveChangesAsync();

        var result = await CreateController(context).ExportPdf(null, null, null, null, null, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(file.FileContents, 0, 4));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ReportsController CreateController(AppDbContext context)
    {
        var controller = new ReportsController(context);
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
}
