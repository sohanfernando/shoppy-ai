namespace AdvancedOrderSystem.Models.Entities;

public class Review
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int CommentMaxLength = 1000;

    public int Id { get; set; }

    public int ProductId { get; set; }

    public int CustomerId { get; set; }

    // 1 to 5 stars
    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;

    public Customer Customer { get; set; } = null!;
}
