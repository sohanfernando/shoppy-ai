namespace AdvancedOrderSystem.Models.DTOs.Review;

public class CreateReviewRequest
{
    public int ProductId { get; set; }

    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;
}

public class ReviewResponse
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class ProductReviewsResponse
{
    public double AverageRating { get; set; }

    public int ReviewCount { get; set; }

    // Whether the signed-in customer may add a review for this product
    public bool CanReview { get; set; }

    public string? CannotReviewReason { get; set; }

    public List<ReviewResponse> Reviews { get; set; } = new();
}
