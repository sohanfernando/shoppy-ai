namespace AdvancedOrderSystem.Models.DTOs.Product;

public class ProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public decimal UnitPrice { get; set; }

    public int Stock { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

}