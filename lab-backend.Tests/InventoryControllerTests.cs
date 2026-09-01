using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LabManagementAPI.Controllers;
using LabManagementAPI.Data;
using LabManagementAPI.Dtos;
using LabManagementAPI.Models;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using Xunit;

namespace LabManagementAPI.Tests;

public sealed class InventoryControllerTests
{
    [Fact]
    public async Task ExportExcel_returns_openable_workbook_with_inventory_status_labels()
    {
        await using var context = CreateContext();
        await SeedSession(context, includeItems: true);

        var result = await CreateController(context).ExportExcel(5, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        Assert.EndsWith(".xlsx", file.FileDownloadName, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(file.FileContents);

        using var package = new ExcelPackage(new MemoryStream(file.FileContents));
        var sheet = Assert.Single(package.Workbook.Worksheets);
        Assert.Equal("ChenhLech", sheet.Name);
        Assert.Equal("Đã tìm thấy", sheet.Cells[2, 5].Text);
        Assert.Equal(string.Empty, sheet.Cells[3, 1].Text);
    }

    [Fact]
    public async Task ExportPdf_returns_valid_pdf_when_inventory_has_no_items()
    {
        await using var context = CreateContext();
        await SeedSession(context, includeItems: false);

        var result = await CreateController(context).ExportPdf(5, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.EndsWith(".pdf", file.FileDownloadName, StringComparison.OrdinalIgnoreCase);
        Assert.True(file.FileContents.Length > 4);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(file.FileContents, 0, 4));
    }

    [Fact]
    public async Task CreateSession_excludes_reserved_borrowed_and_inconsistent_active_borrow_assets()
    {
        await using var context = CreateContext();
        var location = new LocationNode { Id = 1, Code = "LAB", Name = "Phòng Lab", Type = "ROOM" };
        context.LocationNodes.Add(location);
        context.Equipments.AddRange(
            CreateEquipment(1, EquipmentStatuses.Available, location),
            CreateEquipment(2, EquipmentStatuses.BorrowPending, location),
            CreateEquipment(3, EquipmentStatuses.Borrowed, location),
            CreateEquipment(4, EquipmentStatuses.Available, location));
        context.BorrowRecords.Add(new BorrowRecord
        {
            Id = 8,
            UserId = 2,
            Status = BorrowStatuses.Borrowed,
            BorrowDate = DateTime.UtcNow,
            ExpectedReturnDate = DateTime.UtcNow.AddDays(2),
            Purpose = "Bản ghi lệch trạng thái thiết bị",
            Details = [new BorrowRequestDetail { EquipmentId = 4, Status = BorrowStatuses.Borrowed }]
        });
        await context.SaveChangesAsync();

        var result = await CreateController(context).CreateSession(
            new InventoryController.CreateInventoryDto { Name = "Kiểm kê loại trừ tài sản đang mượn" },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        var session = await context.InventorySessions.AsNoTracking().Include(item => item.Items).SingleAsync();
        var item = Assert.Single(session.Items);
        Assert.Equal(1, item.EquipmentId);
    }

    [Fact]
    public async Task Inventory_requires_discrepancy_review_and_syncs_missing_equipment_status()
    {
        await using var context = CreateContext();
        var equipment = CreateEquipment(30, EquipmentStatuses.Available, null);
        context.InventorySessions.Add(new InventorySession
        {
            Id = 15,
            Code = "INV-REVIEW-001",
            Name = "Kiểm kê cần đối soát",
            Status = InventoryStatuses.Open,
            CreatedByUserId = 1,
            Items =
            [
                new InventoryItem
                {
                    Id = 31,
                    Equipment = equipment,
                    EquipmentId = equipment.Id,
                    ExpectedLocationName = "Phòng Lab",
                    Status = InventoryItemStatuses.Pending
                }
            ]
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        Assert.IsType<OkObjectResult>(await controller.StartReview(15, CancellationToken.None));
        Assert.IsType<ConflictObjectResult>(await controller.Complete(15, CancellationToken.None));

        var reviewResult = await controller.ReviewItem(
            15,
            31,
            new InventoryController.ReviewInventoryItemDto
            {
                Resolution = InventoryReviewResolutions.MarkMissing,
                Note = "Đã kiểm tra toàn bộ phòng nhưng không tìm thấy."
            },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(reviewResult);
        Assert.Equal(EquipmentStatuses.Missing, (await context.Equipments.AsNoTracking().SingleAsync()).Status);
        Assert.IsType<OkObjectResult>(await controller.Complete(15, CancellationToken.None));
        Assert.Equal(InventoryStatuses.Completed, (await context.InventorySessions.AsNoTracking().SingleAsync()).Status);
    }

    private static Equipment CreateEquipment(int id, string status, LocationNode? location) => new()
    {
        Id = id,
        AssetCode = $"IOT-{id:000}",
        QrToken = $"qr-{id:000}",
        Name = $"Thiết bị {id}",
        Model = "Model",
        Serial = $"SN-{id:000}",
        Location = location?.Name ?? "Phòng Lab",
        LocationNodeId = location?.Id,
        LocationNode = location,
        Status = status
    };

    [Fact]
    public async Task Session_items_are_paged_instead_of_returning_the_entire_inventory()
    {
        await using var context = CreateContext();
        var session = new InventorySession
        {
            Id = 30,
            Code = "INV-PAGED",
            Name = "Kiểm kê phân trang",
            Status = InventoryStatuses.Open,
            CreatedByUserId = 1
        };
        for (var id = 1; id <= 25; id++)
        {
            session.Items.Add(new InventoryItem
            {
                Id = id,
                EquipmentId = id,
                ExpectedLocationName = "Lab",
                Status = InventoryItemStatuses.Pending,
                Equipment = new Equipment
                {
                    Id = id,
                    AssetCode = $"EQ-{id:000}",
                    QrToken = $"qr-{id:000}",
                    Name = $"Thiết bị {id:000}",
                    Model = "M1",
                    Serial = $"SN-{id:000}",
                    Location = "Lab"
                }
            });
        }
        context.InventorySessions.Add(session);
        await context.SaveChangesAsync();

        var result = await CreateController(context).GetSessionItemsPaged(
            30,
            new PageQuery { Page = 2, PageSize = 10 },
            CancellationToken.None);

        var payload = Assert.IsType<PagedResult<object>>(
            Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(25, payload.Total);
        Assert.Equal(3, payload.TotalPages);
        Assert.Equal(10, payload.Items.Count);
        Assert.Equal(2, payload.Page);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task SeedSession(AppDbContext context, bool includeItems)
    {
        var session = new InventorySession
        {
            Id = 5,
            Code = "INV-TEST-001",
            Name = "Kiểm kê kiểm thử",
            Status = InventoryStatuses.Completed,
            CreatedByUserId = 1
        };

        if (includeItems)
        {
            session.Items.Add(new InventoryItem
            {
                Id = 11,
                EquipmentId = 21,
                ExpectedLocationName = "Phòng Lab A",
                Status = InventoryItemStatuses.Found,
                Equipment = new Equipment
                {
                    Id = 21,
                    AssetCode = "IOT-001",
                    Name = "Thiết bị IoT",
                    Serial = "SN-001"
                }
            });
        }

        context.InventorySessions.Add(session);
        await context.SaveChangesAsync();
    }

    private static InventoryController CreateController(AppDbContext context)
    {
        var controller = new InventoryController(
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
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, Roles.Admin)
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

        public Task NotifyUsersAsync(IEnumerable<int> userIds, string type, string title,
            string message, string url, CancellationToken cancellationToken) => Task.CompletedTask;

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
