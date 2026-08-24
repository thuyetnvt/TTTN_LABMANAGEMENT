using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LabManagementAPI.Controllers;
using LabManagementAPI.Data;
using LabManagementAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LabManagementAPI.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_returns_token_for_active_user_and_rejects_inactive_user()
    {
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        var active = TestUser(1, "active-user", "SinhVien", true);
        var inactive = TestUser(2, "inactive-user", "SinhVien", false);
        context.Users.AddRange(active, inactive);
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var success = await controller.Login(
            new AuthController.LoginRequest { Username = " active-user ", Password = "Password123!" },
            CancellationToken.None);
        var unauthorized = await controller.Login(
            new AuthController.LoginRequest { Username = "inactive-user", Password = "Password123!" },
            CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(success);
        Assert.NotNull(response.Value);
        Assert.IsType<UnauthorizedObjectResult>(unauthorized);
    }

    [Fact]
    public async Task ForgotPassword_keeps_response_generic_for_unknown_email()
    {
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        var controller = CreateController(context);

        var result = await controller.ForgotPassword(
            new AuthController.ForgotPasswordRequest { Email = "unknown@example.test" },
            CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Nếu email tồn tại", response.Value!.ToString());
        Assert.Empty(context.PasswordResetTokens);
    }

    [Fact]
    public async Task ResetPassword_changes_password_increments_token_version_and_consumes_token()
    {
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        const string rawToken = "reset-token-for-test";
        var user = TestUser(1, "reset-user", "SinhVien", true);
        user.TokenVersion = 4;
        context.Users.Add(user);
        context.PasswordResetTokens.Add(new LabManagementAPI.Models.PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.ResetPassword(
            new AuthController.ResetPasswordRequest { Token = rawToken, NewPassword = "NewPassword123!" },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var updatedUser = await context.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);
        var consumedToken = await context.PasswordResetTokens.AsNoTracking().SingleAsync();
        Assert.Equal(5, updatedUser.TokenVersion);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword123!", updatedUser.PasswordHash));
        Assert.NotNull(consumedToken.UsedAt);

        var secondAttempt = await controller.ResetPassword(
            new AuthController.ResetPasswordRequest { Token = rawToken, NewPassword = "AnotherPassword123!" },
            CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(secondAttempt);
    }

    private static AppDbContext CreateContext(Microsoft.Data.Sqlite.SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static AuthController CreateController(AppDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-key-that-is-long-enough-for-hmac-sha256",
                ["Jwt:Issuer"] = "LabManagement.Tests",
                ["Jwt:Audience"] = "LabManagement.Tests",
                ["Jwt:AccessTokenMinutes"] = "30",
                ["App:FrontendBaseUrl"] = "http://localhost:4173"
            })
            .Build();
        var controller = new AuthController(context, configuration, new NoopEmailService(), new NoopAuditService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity("Test"))
            }
        };
        return controller;
    }

    private static LabManagementAPI.Models.User TestUser(int id, string username, string role, bool isActive)
        => new()
        {
            Id = id,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FullName = username,
            Role = role,
            IsActive = isActive,
            Email = $"{username}@example.test"
        };

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

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
}
