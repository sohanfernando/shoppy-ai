using System.Security.Claims;
using AdvancedOrderSystem.Auth;
using AdvancedOrderSystem.Data;
using AdvancedOrderSystem.Exceptions;
using AdvancedOrderSystem.Hubs;
using AdvancedOrderSystem.Models.DTOs.Notification;
using AdvancedOrderSystem.Models.Entities;
using AdvancedOrderSystem.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace AdvancedOrderSystem.Services.Impl;

public class NotificationService : INotificationService
{
    private const int RecentCount = 30;
    private const string ReceiveMethod = "notification";

    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        IHubContext<NotificationHub> hub)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _hub = hub;
    }

    public async Task<NotificationListResponse> GetMineAsync(ClaimsPrincipal principal)
    {
        var (userId, isAdmin) = Identify(principal);

        var notifications =
            await _notificationRepository.GetForUserAsync(userId, isAdmin, RecentCount);

        return new NotificationListResponse
        {
            Items = notifications.Select(MapToResponse).ToList(),
            UnreadCount = await _notificationRepository.CountUnreadAsync(userId, isAdmin)
        };
    }

    public async Task MarkAsReadAsync(ClaimsPrincipal principal, int id)
    {
        var (userId, isAdmin) = Identify(principal);

        var notification = await _notificationRepository.GetByIdAsync(id);

        if (notification == null)
        {
            throw new KeyNotFoundException($"Notification with id {id} was not found.");
        }

        var isMine = isAdmin
            ? notification.RecipientUserId == null
            : notification.RecipientUserId == userId;

        if (!isMine)
        {
            throw new ForbiddenException("This notification does not belong to you.");
        }

        notification.IsRead = true;

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(ClaimsPrincipal principal)
    {
        var (userId, isAdmin) = Identify(principal);

        foreach (var notification in await _notificationRepository.GetUnreadAsync(userId, isAdmin))
        {
            notification.IsRead = true;
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task NotifyAdminsAsync(string type, string title, string message, string link)
    {
        var notification = await SaveAsync(null, type, title, message, link);

        await _hub.Clients
            .Group(AuthConstants.AdminNotificationGroup)
            .SendAsync(ReceiveMethod, MapToResponse(notification));
    }

    public async Task NotifyUserAsync(
        int userId, string type, string title, string message, string link)
    {
        var notification = await SaveAsync(userId, type, title, message, link);

        await _hub.Clients
            .User(userId.ToString())
            .SendAsync(ReceiveMethod, MapToResponse(notification));
    }

    private async Task<Notification> SaveAsync(
        int? recipientUserId, string type, string title, string message, string link)
    {
        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            Type = type,
            Title = title,
            Message = message,
            Link = link,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    private static (int UserId, bool IsAdmin) Identify(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var userId))
        {
            throw new AuthenticationFailedException("Your session is no longer valid.");
        }

        return (userId, principal.IsInRole(AuthConstants.AdminRole));
    }

    private static NotificationResponse MapToResponse(Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            Link = notification.Link,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }
}
