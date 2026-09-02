using System;
using System.Linq;
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

public class AuditLoggingTests
{
    [Fact]
    public async Task Anonymous_authentication_logs_resolve_the_actor_name()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        context.Users.Add(new User
        {
            Id = 7,
            Username = "admin",
            PasswordHash = "not-used",
            FullName = "Quản trị viên",
            Role = Roles.Admin,
            IsActive = true
        });
        await context.SaveChangesAsync();
        var service = new AuditService(context);
        var httpContext = new DefaultHttpContext();

        await service.WriteAsync(httpContext, "LoginSucceeded", nameof(User), 7);
        await service.WriteAsync(
            httpContext,
            "LoginFailed",
            nameof(User),
            details: new { Username = "unknown-account" });

        var logs = await context.AuditLogs.OrderBy(item => item.Id).ToListAsync();
        Assert.Equal(7, logs[0].UserId);
        Assert.Equal("admin", logs[0].Username);
        Assert.Null(logs[1].UserId);
        Assert.Equal("unknown-account", logs[1].Username);
    }

    [Fact]
    public async Task GetLogs_restores_actor_name_for_existing_anonymous_login_log()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        context.Users.Add(new User
        {
            Id = 3,
            Username = "pholab",
            PasswordHash = "not-used",
            FullName = "Phó phòng lab",
            Role = Roles.DeputyLabHead,
            IsActive = true
        });
        context.AuditLogs.Add(new AuditLog
        {
            Action = "LoginSucceeded",
            EntityType = nameof(User),
            EntityId = "3",
            Username = string.Empty,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var controller = new AuditController(context);

        var result = await controller.GetLogs(cancellationToken: CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(
            response.Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var item = json.RootElement.GetProperty("items")[0];
        Assert.Equal("pholab", item.GetProperty("username").GetString());
    }

    private static AppDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
