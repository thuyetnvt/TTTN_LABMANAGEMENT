using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
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
using Xunit;

namespace LabManagementAPI.Tests;

public sealed class EquipmentControllerTests
{
    [Theory]
    [InlineData(Roles.Student)]
    [InlineData(Roles.Teacher)]
    public async Task Borrower_list_uses_safe_dto_without_qr_or_financial_fields(string role)
    {
        await using var context = CreateContext();
        context.Equipments.Add(CreateSensitiveEquipment());
        await context.SaveChangesAsync();

        var result = await CreateController(context, role).GetEquipments(CancellationToken.None);

        var payload = Assert.IsType<OkObjectResult>(result).Value;
        var item = Assert.Single(Assert.IsAssignableFrom<IEnumerable<BorrowerEquipmentDto>>(payload));
        Assert.IsNotType<ManagerEquipmentDto>(item);
        var json = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web);
        Assert.DoesNotContain("qrToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("purchaseValue", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("supplier", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("invoiceNumber", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fundingSource", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("responsiblePerson", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.LabHead)]
    [InlineData(Roles.DeputyLabHead)]
    public async Task Manager_list_uses_full_management_dto(string role)
    {
        await using var context = CreateContext();
        context.Equipments.Add(CreateSensitiveEquipment());
        await context.SaveChangesAsync();

        var result = await CreateController(context, role).GetEquipments(CancellationToken.None);

        var payload = Assert.IsType<OkObjectResult>(result).Value;
        var item = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ManagerEquipmentDto>>(payload));
        Assert.Equal("secret-qr-token", item.QrToken);
        Assert.Equal(12500000, item.PurchaseValue);
        Assert.Equal("NCC nội bộ", item.Supplier);
    }

    [Fact]
    public async Task Resolve_qr_returns_safe_dto_and_never_echoes_token()
    {
        await using var context = CreateContext();
        context.Equipments.Add(CreateSensitiveEquipment());
        await context.SaveChangesAsync();

        var result = await CreateController(context, Roles.Admin).ResolveQr(
            new EquipmentController.ResolveEquipmentQrDto { QrToken = "secret-qr-token" },
            CancellationToken.None);

        var payload = Assert.IsType<OkObjectResult>(result).Value;
        Assert.IsType<BorrowerEquipmentDto>(payload);
        var json = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web);
        Assert.DoesNotContain("qrToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret-qr-token", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Paged_list_filters_on_server_and_returns_exact_total()
    {
        await using var context = CreateContext();
        for (var id = 1; id <= 25; id++)
        {
            context.Equipments.Add(new Equipment
            {
                Id = id,
                AssetCode = $"EQ-{id:000}",
                QrToken = $"qr-{id:000}",
                Name = id <= 12 ? $"Cảm biến {id:000}" : $"Máy đo {id:000}",
                Model = "M1",
                Serial = $"SN-{id:000}",
                Location = "Lab",
                Status = EquipmentStatuses.Available,
                CreatedAt = new DateTime(2026, 9, 1).AddMinutes(id)
            });
        }
        await context.SaveChangesAsync();

        var result = await CreateController(context, Roles.Admin).GetEquipmentsPaged(
            new PageQuery { Page = 2, PageSize = 5, Search = "Cảm biến" },
            CancellationToken.None);

        var payload = Assert.IsType<PagedResult<ManagerEquipmentDto>>(
            Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(12, payload.Total);
        Assert.Equal(3, payload.TotalPages);
        Assert.Equal(5, payload.Items.Count);
        Assert.Equal(2, payload.Page);
        Assert.All(payload.Items, item => Assert.Contains("Cảm biến", item.Name));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static Equipment CreateSensitiveEquipment() => new()
    {
        Id = 1,
        AssetCode = "IOT-001",
        QrToken = "secret-qr-token",
        Name = "Gateway IoT",
        Model = "GW-01",
        Serial = "SN-001",
        Location = "Phòng Lab IoT",
        Status = EquipmentStatuses.Available,
        Supplier = "NCC nội bộ",
        FundingSource = "Ngân sách khoa",
        PurchaseValue = 12500000,
        InvoiceNumber = "INV-SECRET",
        ResponsiblePerson = "CBNV01",
        Notes = "Ghi chú quản trị"
    };

    private static EquipmentController CreateController(AppDbContext context, string role)
    {
        var controller = new EquipmentController(
            context,
            new ConfigurationBuilder().Build(),
            new NoopAuditService(),
            new NoopFileStorage());
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
        return controller;
    }

    private sealed class NoopAuditService : IAuditService
    {
        public Task WriteAsync(HttpContext httpContext, string action, string entityType,
            object? entityId = null, object? details = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
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
