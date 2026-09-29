using AdvancedOrderSystem.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AdvancedOrderSystem.Hubs;

// Clients only listen; notifications are pushed from the services
[Authorize(Policy = AuthConstants.SignedInPolicy)]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Admins share one group; customers are addressed by their user id
        if (Context.User?.IsInRole(AuthConstants.AdminRole) == true)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId, AuthConstants.AdminNotificationGroup);
        }

        await base.OnConnectedAsync();
    }
}
