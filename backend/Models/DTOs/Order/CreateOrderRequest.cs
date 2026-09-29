namespace AdvancedOrderSystem.Models.DTOs.Order;

// The customer comes from the signed-in account, never from the request
public class CreateOrderRequest
{
    public decimal DiscountPercent { get; set; }

    public List<CreateOrderItemRequest> Items { get; set; } = new();
}