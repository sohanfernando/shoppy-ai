using System.Security.Claims;
using AdvancedOrderSystem.Models.DTOs.Notification;

namespace AdvancedOrderSystem.Services;

public interface INotificationService
{
    Task<NotificationListResponse> GetMineAsync(ClaimsPrincipal principal);

    Task MarkAsReadAsync(ClaimsPrincipal principal, int id);

    Task MarkAllAsReadAsync(ClaimsPrincipal principal);

    // Saves the notification, then pushes it over SignalR
    Task NotifyAdminsAsync(string type, string title, string message, string link);

    Task NotifyUserAsync(int userId, string type, string title, string message, string link);
}
