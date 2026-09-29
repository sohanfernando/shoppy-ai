namespace AdvancedOrderSystem.Models.Entities;

public static class NotificationType
{
    public const string OrderPlaced = "order-placed";
    public const string OrderCancelled = "order-cancelled";
    public const string ReviewPosted = "review-posted";
}

public class Notification
{
    public int Id { get; set; }

    // null = every admin sees it; otherwise the one account it belongs to
    public int? RecipientUserId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    // Frontend path to open when the notification is clicked
    public string Link { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public AppUser? RecipientUser { get; set; }
}
