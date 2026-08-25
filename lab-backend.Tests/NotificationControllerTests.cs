using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LabManagementAPI.Controllers;
using LabManagementAPI.Data;
using LabManagementAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LabManagementAPI.Tests;

public sealed class NotificationControllerTests
{
    [Fact]
    public async Task Get_supports_pagination_and_unread_filter_per_user()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User { Id = 1, Username = "user-a", Role = Roles.Student },
            new User { Id = 2, Username = "user-b", Role = Roles.Student });
        context.Notifications.AddRange(
            new AppNotification { Id = 1, UserId = 1, Title = "Unread one", Message = "A" },
            new AppNotification { Id = 2, UserId = 1, Title = "Read one", Message = "B", IsRead = true },
            new AppNotification { Id = 3, UserId = 2, Title = "Other user", Message = "C" });
        await context.SaveChangesAsync();

        var result = await CreateController(context, 1).Get(1, 1, true, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var payload = JsonSerializer.Serialize(response.Value);
        Assert.Contains("Unread one", payload);
        Assert.DoesNotContain("Read one", payload);
        Assert.DoesNotContain("Other user", payload);
        Assert.Contains("HasNextPage", payload);
    }

    [Fact]
    public async Task MarkRead_is_idempotent_but_does_not_cross_user_boundary()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User { Id = 1, Username = "user-a", Role = Roles.Student },
            new User { Id = 2, Username = "user-b", Role = Roles.Student });
        context.Notifications.AddRange(
            new AppNotification { Id = 1, UserId = 1, Title = "Already read", IsRead = true },
            new AppNotification { Id = 2, UserId = 2, Title = "Private" });
        await context.SaveChangesAsync();

        var controller = CreateController(context, 1);
        Assert.IsType<NoContentResult>(await controller.MarkRead(1, CancellationToken.None));
        Assert.IsType<NotFoundObjectResult>(await controller.MarkRead(2, CancellationToken.None));
    }

    [Fact]
    public async Task MarkAllRead_updates_only_the_current_users_unread_items()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User { Id = 1, Username = "user-a", Role = Roles.Student },
            new User { Id = 2, Username = "user-b", Role = Roles.Student });
        context.Notifications.AddRange(
            new AppNotification { Id = 1, UserId = 1, Title = "Unread A" },
            new AppNotification { Id = 2, UserId = 1, Title = "Read A", IsRead = true },
            new AppNotification { Id = 3, UserId = 2, Title = "Unread B" });
        await context.SaveChangesAsync();

        Assert.IsType<NoContentResult>(await CreateController(context, 1).MarkAllRead(CancellationToken.None));

        Assert.All(await context.Notifications.Where(item => item.UserId == 1).ToListAsync(), item => Assert.True(item.IsRead));
        Assert.False((await context.Notifications.SingleAsync(item => item.Id == 3)).IsRead);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static NotificationController CreateController(AppDbContext context, int userId)
    {
        var controller = new NotificationController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test"))
                }
            }
        };
        return controller;
    }
}
