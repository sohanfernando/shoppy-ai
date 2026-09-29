using System.Security.Claims;
using AdvancedOrderSystem.Models.DTOs.Common;
using AdvancedOrderSystem.Models.DTOs.Order;

namespace AdvancedOrderSystem.Services;

public interface IOrderService
{
    // The order is always placed for the signed-in customer
    Task<OrderResponse> CreateOrderAsync(
        ClaimsPrincipal principal,
        CreateOrderRequest request);

    // Customers may only open their own orders
    Task<OrderResponse?> GetOrderByIdAsync(
        int id,
        ClaimsPrincipal principal);

    // Admin view of every order
    Task<PagedResponse<OrderListResponse>> GetOrdersAsync(
        string? status,
        int? customerId,
        int page,
        int pageSize);

    // The signed-in customer's own orders
    Task<PagedResponse<OrderListResponse>> GetMyOrdersAsync(
        ClaimsPrincipal principal,
        string? status,
        int page,
        int pageSize);

    Task<OrderResponse> CancelOrderAsync(
        int id,
        ClaimsPrincipal principal);
}
