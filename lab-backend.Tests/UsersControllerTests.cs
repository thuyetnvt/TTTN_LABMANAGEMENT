using System.Security.Claims;
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
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LabManagementAPI.Tests;

public class UsersControllerTests
{
    [Fact]
    public async Task UpdateUser_keeps_session_for_profile_changes_and_invalidates_it_for_password_changes()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        var user = new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FullName = "Tên cũ",
            Email = "admin@lab.local",
            UniversityCode = "ADMIN101",
            Role = Roles.Admin,
            IsActive = true,
            TokenVersion = 4
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var controller = CreateController(context, user.Id);

        var profileResult = await controller.UpdateUser(
            user.Id,
            CreateUpdateDto(fullName: "Tên mới"),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(profileResult);
        Assert.Equal(4, user.TokenVersion);
        Assert.Equal("Tên mới", user.FullName);

        var passwordResult = await controller.UpdateUser(
            user.Id,
            CreateUpdateDto(fullName: "Tên mới", password: "NewPassword123!"),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(passwordResult);
        Assert.Equal(5, user.TokenVersion);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword123!", user.PasswordHash));
    }

    private static UsersController.UpdateUserDto CreateUpdateDto(string fullName, string? password = null)
        => new()
        {
            Username = "admin",
            Email = "admin@lab.local",
            FullName = fullName,
            UniversityCode = "ADMIN101",
            Phone = string.Empty,
            Department = string.Empty,
            Role = Roles.Admin,
            Password = password
        };

    private static UsersController CreateController(AppDbContext context, int userId)
    {
        var controller = new UsersController(
            context,
            new NoopAuditService(),
            null!,
            NullLogger<UsersController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, "admin"),
                    new Claim(ClaimTypes.Role, Roles.Admin)
                ], "Test"))
            }
        };
        return controller;
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

    private sealed class NoopAuditService : IAuditService
    {
        public Task WriteAsync(
            HttpContext httpContext,
            string action,
            string entityType,
            object? entityId = null,
            object? details = null,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
