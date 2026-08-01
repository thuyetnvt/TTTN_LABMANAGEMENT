using System.Security.Claims;
using LabManagementAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LabManagementAPI.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    public const string ManagerGroup = "LabManagers";

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        if (role is Roles.Admin or Roles.LabHead or Roles.DeputyLabHead)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ManagerGroup);
        }

        await base.OnConnectedAsync();
    }
}
