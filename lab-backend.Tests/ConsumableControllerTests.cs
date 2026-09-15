using System;
using System.Collections.Generic;
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

    [Fact]
    public async Task Manager_list_shows_responsible_identity_without_private_user_data()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            context.Users.AddRange(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    FullName = "Nguyễn Văn Admin",
                    UniversityCode = "CB001",
                    Email = "admin@example.test",
                    Phone = "0900000001",
                    Role = Roles.Admin
                },
                new User
                {
                    Id = 2,
                    Username = "truonglab",
                    FullName = "Trần Thị Trưởng Lab",
                    UniversityCode = "CB002",
                    Email = "truonglab@example.test",
                    Phone = "0900000002",
                    Role = Roles.LabHead
                });
            context.Consumables.Add(new Consumable
            {
                Id = 2,
                Code = "VT-002",
                Name = "Thiếc hàn",
                Unit = "cuộn",
                Quantity = 4,
                CreatedByUserId = 1,
                ResponsibleUserId = 2,
                ResponsiblePerson = "Dữ liệu cũ"
            });
            await context.SaveChangesAsync();

            var result = Assert.IsType<OkObjectResult>(
                (await CreateController(context, Roles.Admin).GetConsumables(CancellationToken.None)).Result);
            var rows = Assert.IsAssignableFrom<IEnumerable<ManagerConsumableDto>>(result.Value);
            var row = Assert.Single(rows);
            var json = JsonSerializer.Serialize(result.Value);

            Assert.Contains("responsibleUserId", json, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("Trần Thị Trưởng Lab", row.ResponsibleName);
            Assert.Equal("CB002", row.ResponsibleCode);
            Assert.Contains("createdByUserId", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("admin@example.test", json, StringComparison.Ordinal);
            Assert.DoesNotContain("truonglab@example.test", json, StringComparison.Ordinal);
            Assert.DoesNotContain("0900000002", json, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Transaction_history_localizes_legacy_types_and_cleans_internal_seed_text()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            context.Users.Add(new User
            {
                Id = 1,
                Username = "admin",
                FullName = "Nguyễn Văn Admin",
                Role = Roles.Admin
            });
            await SeedConsumableAsync(context);
            context.ConsumableTransactions.AddRange(
                new ConsumableTransaction
                {
                    ConsumableId = 1,
                    Type = "IN",
                    Quantity = 10,
                    BeforeQuantity = 0,
                    AfterQuantity = 10,
                    Reason = "[SEED-FULL] Nhập lô vật tư mẫu",
                    UserId = 1,
                    CreatedAt = new DateTime(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc)
                },
                new ConsumableTransaction
                {
                    ConsumableId = 1,
                    Type = "OUT",
                    Quantity = 2,
                    BeforeQuantity = 10,
                    AfterQuantity = 8,
                    Reason = "Xuất vật tư cho buổi thực hành",
                    UserId = 1,
                    CreatedAt = new DateTime(2026, 9, 13, 8, 0, 0, DateTimeKind.Utc)
                });
            await context.SaveChangesAsync();

            var result = Assert.IsType<OkObjectResult>(
                (await CreateController(context, Roles.Admin)
                    .GetTransactions(1, CancellationToken.None)).Result);
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
            var rows = document.RootElement;

            Assert.Equal("Nhập kho", rows[0].GetProperty("Type").GetString());
            Assert.Equal("Nhập lô vật tư mẫu", rows[0].GetProperty("Reason").GetString());
            Assert.Equal("Nguyễn Văn Admin", rows[0].GetProperty("PerformedBy").GetString());
            Assert.Equal("Xuất kho", rows[1].GetProperty("Type").GetString());
        }
    }

    [Fact]
    public async Task Creating_consumable_uses_current_user_and_defaults_responsible_user_to_creator()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            context.Users.Add(new User
            {
                Id = 1,
                Username = "admin",
                FullName = "Nguyễn Văn Admin",
                UniversityCode = "CB001",
                Role = Roles.Admin
            });
            await context.SaveChangesAsync();

            var dto = new ConsumableController.ConsumableDto
            {
                Name = "Dây điện",
                Unit = "mét",
                Quantity = 0,
                MinQuantity = 2,
                ResponsiblePerson = "Tên tự nhập không được dùng"
            };

            var result = await CreateController(context, Roles.Admin)
                .PostConsumable(dto, CancellationToken.None);

            Assert.IsType<OkObjectResult>(result.Result);
            var consumable = await context.Consumables.SingleAsync();
            Assert.Equal(1, consumable.CreatedByUserId);
            Assert.Equal(1, consumable.ResponsibleUserId);
            Assert.Equal("Nguyễn Văn Admin", consumable.ResponsiblePerson);
        }
    }

    [Fact]
    public async Task Manager_can_assign_an_active_manager_as_responsible_user()
    {
        await using var context = CreateContext(out var connection);
        await using (connection)
        {
            context.Users.AddRange(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    FullName = "Nguyễn Văn Admin",
                    Role = Roles.Admin
                },
                new User
                {
                    Id = 2,
                    Username = "pholab",
                    FullName = "Lê Thị Phó Lab",
                    Role = Roles.DeputyLabHead
                });
            await context.SaveChangesAsync();

            var result = await CreateController(context, Roles.Admin)
                .PostConsumable(new ConsumableController.ConsumableDto
                {
                    Name = "Băng keo điện",
                    Unit = "cuộn",
                    Quantity = 0,
                    MinQuantity = 1,
                    ResponsibleUserId = 2
                }, CancellationToken.None);

            Assert.IsType<OkObjectResult>(result.Result);
            var consumable = await context.Consumables.SingleAsync();
            Assert.Equal(1, consumable.CreatedByUserId);
            Assert.Equal(2, consumable.ResponsibleUserId);
            Assert.Equal("Lê Thị Phó Lab", consumable.ResponsiblePerson);
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
