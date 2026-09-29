using AdvancedOrderSystem.Auth;
using AdvancedOrderSystem.Models.DTOs.Common;
using AdvancedOrderSystem.Models.DTOs.Order;
using AdvancedOrderSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdvancedOrderSystem.Controllers;

// Errors are mapped to status codes by GlobalExceptionHandler
[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(
        IOrderService orderService)
    {
        _orderService = orderService;
    }

    // ==================================
    // Customers place their own orders
    // ==================================

    [HttpPost]
    [Authorize(Policy = AuthConstants.CustomerPolicy)]
    public async Task<ActionResult<OrderResponse>>
        CreateOrder(
            CreateOrderRequest request)
    {
        var order =
            await _orderService
                .CreateOrderAsync(User, request);

        return CreatedAtAction(
            nameof(GetOrderById),
            new { id = order.Id },
            order
        );
    }

    // The signed-in customer's own orders
    [HttpGet("my")]
    [Authorize(Policy = AuthConstants.CustomerPolicy)]
    public async Task<ActionResult<PagedResponse<OrderListResponse>>> GetMyOrders(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return Ok(await _orderService.GetMyOrdersAsync(User, status, page, pageSize));
    }

    // ==================================
    // Shared: admins see any order, customers only their own
    // ==================================

    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthConstants.SignedInPolicy)]
    public async Task<ActionResult<OrderResponse>>
        GetOrderById(int id)
    {
        var order =
            await _orderService
                .GetOrderByIdAsync(id, User);

        if (order == null)
        {
            return NotFound(new
            {
                message =
                    $"Order with id {id} was not found."
            });
        }

        return Ok(order);
    }

    [HttpPatch("{id:int}/cancel")]
    [Authorize(Policy = AuthConstants.SignedInPolicy)]
    public async Task<ActionResult<OrderResponse>>
        CancelOrder(int id)
    {
        var order =
            await _orderService
                .CancelOrderAsync(id, User);

        return Ok(order);
    }

    // ==================================
    // Admin: every order
    // ==================================

    [HttpGet]
    [Authorize(Policy = AuthConstants.AdminPolicy)]
    public async Task<
        ActionResult<
            PagedResponse<OrderListResponse>>>
        GetOrders(
            [FromQuery] string? status,
            [FromQuery] int? customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
    {
        var result =
            await _orderService
                .GetOrdersAsync(
                    status,
                    customerId,
                    page,
                    pageSize
                );

        return Ok(result);
    }
}
