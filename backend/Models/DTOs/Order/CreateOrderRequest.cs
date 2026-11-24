namespace AdvancedOrderSystem.Models.DTOs.Order;

// The customer comes from the signed-in account, never from the request
public class CreateOrderRequest
{
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}