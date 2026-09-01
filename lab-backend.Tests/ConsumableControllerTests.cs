using System;
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
using Xunit;

namespace LabManagementAPI.Tests;

public sealed class ConsumableControllerTests
{
    [Fact]
    public async Task Borrower_list_does_not_expose_financial_supplier_or_lot_data()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            await SeedConsumableAsync(context);
            var controller = CreateController(context, Roles.Student);

            var result = Assert.IsType<OkObjectResult>(
                (await controller.GetConsumables(CancellationToken.None)).Result);
            var json = JsonSerializer.Serialize(result.Value);

            Assert.DoesNotContain("UnitCost", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Supplier", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("InvoiceNumber", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("LotNumber", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ResponsiblePerson", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("availableQuantity", json, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Manager_list_contains_inventory_management_fields()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            await SeedConsumableAsync(context);
            var controller = CreateController(context, Roles.LabHead);

            var result = Assert.IsType<OkObjectResult>(
                (await controller.GetConsumables(CancellationToken.None)).Result);
            var json = JsonSerializer.Serialize(result.Value);

            Assert.Contains("UnitCost", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ReservedQuantity", json, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("lotCount", json, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task SeedConsumableAsync(AppDbContext context)
    {
        context.Consumables.Add(new Consumable
        {
            Id = 1,
            Code = "VT-001",
            Name = "Điện trở",
            Unit = "cái",
            Quantity = 10,
            ReservedQuantity = 2,
            ResponsiblePerson = "Quản lý",
            Supplier = "Nhà cung cấp",
            InvoiceNumber = "HD-001",
            UnitCost = 5000,
            LotNumber = "LOT-001"
        });
        context.ConsumableLots.Add(new ConsumableLot
        {
            Id = 1,
            ConsumableId = 1,
            LotNumber = "LOT-001",
            InitialQuantity = 10,
            Quantity = 10,
            EntryDate = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private static AppDbContext CreateContext(out SqliteConnection connection)
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options);
        context.Database.EnsureCreated();
        return context;
    }

    private static ConsumableController CreateController(AppDbContext context, string role)
    {
        var controller = new ConsumableController(context, new NoopAuditService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Role, role)],
                    "Test"))
            }
        };
        return controller;
    }

    private sealed class NoopAuditService : IAuditService
    {
        public Task WriteAsync(
            HttpContext httpContext,
            string action,
            string entityType,
            object? entityId = null,
            object? details = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
