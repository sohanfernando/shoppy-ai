namespace AdvancedOrderSystem.Models.DTOs.Notification;

public class NotificationResponse
{
    public int Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Link { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class NotificationListResponse
{
    public List<NotificationResponse> Items { get; set; } = new();

    public int UnreadCount { get; set; }
}
