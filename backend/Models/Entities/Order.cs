namespace AdvancedOrderSystem.Models.Entities;

public class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public Guid? CheckoutId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Total { get; set; }

    public string Status { get; set; } = string.Empty;

    // Many orders belongs to One Customer
    public Customer Customer { get; set; } = null!;

    //One Order contains Many OrderItems
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
