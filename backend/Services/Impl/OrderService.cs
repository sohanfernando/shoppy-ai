using System.Security.Claims;
using AdvancedOrderSystem.Auth;
using AdvancedOrderSystem.Data;
using AdvancedOrderSystem.Exceptions;
using AdvancedOrderSystem.Models.DTOs.Common;
using AdvancedOrderSystem.Models.DTOs.Order;
using AdvancedOrderSystem.Models.Entities;
using AdvancedOrderSystem.Repositories;

namespace AdvancedOrderSystem.Services.Impl;

public class OrderService : IOrderService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentCustomerService _currentCustomer;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ICurrentCustomerService currentCustomer,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _currentCustomer = currentCustomer;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderResponse> CreateOrderAsync(
        ClaimsPrincipal principal,
        CreateOrderRequest request)
    {
        // The order always belongs to the signed-in customer
        var customer = await _currentCustomer.GetAsync(principal);

        ValidateCreateOrderRequest(request);

        // Get all requested products

        var productIds = request.Items
            .Select(item => item.ProductId)
            .ToList();

        var products =
            await _productRepository
                .GetByIdsAsync(productIds);

        ValidateProductsAvailable(request.Items, products);

        // ==============================
        // Task 9 - Transaction
        // ==============================

        // Disposing an uncommitted transaction rolls it back
        await using var transaction =
            await _unitOfWork.BeginTransactionAsync();

        decimal subtotal = 0;

        var order = new Order
        {
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Confirmed
        };

        // Process products in id order so concurrent orders lock rows in the same order
        foreach (var requestItem in request.Items.OrderBy(item => item.ProductId))
        {
            var product = products
                .First(p =>
                    p.Id == requestItem.ProductId);

            // ==============================
            // Task 8 - Reduce stock
            // ==============================

            // The stock check and update run as one statement, so parallel orders cannot oversell
            var stockReserved =
                await _productRepository
                    .TryDecreaseStockAsync(product.Id, requestItem.Quantity);

            if (!stockReserved)
            {
                throw new ArgumentException(
                    $"Quantity for product {product.Name} exceeds available stock.");
            }

            // ==============================
            // Task 7 - Calculate totals
            // ==============================

            var lineTotal =
                product.UnitPrice *
                requestItem.Quantity;

            lineTotal =
                Math.Round(lineTotal, 2);

            subtotal += lineTotal;

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Quantity = requestItem.Quantity,

                // Store price at purchase time
                UnitPrice = product.UnitPrice,

                LineTotal = lineTotal
            };

            order.OrderItems.Add(orderItem);
        }

        subtotal =
            Math.Round(subtotal, 2);

        var discountAmount =
            subtotal *
            (request.DiscountPercent / 100m);

        discountAmount =
            Math.Round(discountAmount, 2);

        var total =
            subtotal - discountAmount;

        total = Math.Round(total, 2);

        order.Subtotal = subtotal;
        order.DiscountAmount = discountAmount;
        order.Total = total;

        await _orderRepository.AddAsync(order);

        await _unitOfWork.SaveChangesAsync();

        await transaction.CommitAsync();

        // Reload with relationships
        var savedOrder =
            await _orderRepository
                .GetByIdWithDetailsAsync(order.Id);

        await _notificationService.NotifyAdminsAsync(
            NotificationType.OrderPlaced,
            "New order placed",
            $"{customer.Name} ordered {order.OrderItems.Count} item(s) for {order.Total:N2}.",
            $"/admin/orders/{order.Id}");

        return MapToOrderResponse(savedOrder!);
    }

    // ========================================
    // Task 10 - Get one order
    // ========================================

    public async Task<OrderResponse?>
        GetOrderByIdAsync(int id, ClaimsPrincipal principal)
    {
        var order =
            await _orderRepository
                .GetByIdWithDetailsAsync(id);

        if (order == null)
        {
            return null;
        }

        await EnsureCanAccessAsync(order, principal);

        return MapToOrderResponse(order);
    }

    public async Task<PagedResponse<OrderListResponse>> GetMyOrdersAsync(
        ClaimsPrincipal principal,
        string? status,
        int page,
        int pageSize)
    {
        var customer = await _currentCustomer.GetAsync(principal);

        return await GetOrdersAsync(status, customer.Id, page, pageSize);
    }

    // ========================================
    // Task 11 - List orders
    // ========================================

    public async Task<PagedResponse<OrderListResponse>>
        GetOrdersAsync(
            string? status,
            int? customerId,
            int page,
            int pageSize)
    {
        if (pageSize < 1)
        {
            pageSize = DefaultPageSize;
        }

        if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        // Keep (page - 1) * pageSize within int range
        var maxPage = int.MaxValue / pageSize;

        page = Math.Clamp(page, 1, maxPage);

        var orders =
            await _orderRepository.GetAllAsync(
                status,
                customerId,
                page,
                pageSize
            );

        var totalItems =
            await _orderRepository.CountAsync(
                status,
                customerId
            );

        return new PagedResponse<OrderListResponse>
        {
            Items = orders
                .Select(order =>
                    new OrderListResponse
                    {
                        Id = order.Id,
                        CustomerName =
                            order.Customer.Name,
                        OrderDate =
                            order.OrderDate,
                        Total =
                            order.Total,
                        Status =
                            order.Status
                    })
                .ToList(),

            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,

            TotalPages =
                (int)Math.Ceiling(
                    totalItems /
                    (double)pageSize
                )
        };
    }

    // ========================================
    // Task 12 - Cancel order
    // ========================================

    public async Task<OrderResponse>
        CancelOrderAsync(int id, ClaimsPrincipal principal)
    {
        var order =
            await _orderRepository
                .GetByIdWithDetailsAsync(id);

        if (order == null)
        {
            throw new KeyNotFoundException(
                $"Order with id {id} was not found.");
        }

        await EnsureCanAccessAsync(order, principal);

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new ArgumentException(
                "Order is already cancelled.");
        }

        // Disposing an uncommitted transaction rolls it back
        await using var transaction =
            await _unitOfWork.BeginTransactionAsync();

        // Only one concurrent cancel can succeed, so stock is restored exactly once
        var cancelled =
            await _orderRepository.TryCancelAsync(id);

        if (!cancelled)
        {
            throw new ArgumentException(
                "Order is already cancelled.");
        }

        foreach (var item in order.OrderItems.OrderBy(item => item.ProductId))
        {
            await _productRepository
                .IncreaseStockAsync(item.ProductId, item.Quantity);
        }

        await transaction.CommitAsync();

        order.Status = OrderStatus.Cancelled;

        await NotifyCancelledAsync(order, principal);

        return MapToOrderResponse(order);
    }

    // An admin reaches every order; a customer only their own
    private async Task EnsureCanAccessAsync(Order order, ClaimsPrincipal principal)
    {
        if (principal.IsInRole(AuthConstants.AdminRole))
        {
            return;
        }

        var customer = await _currentCustomer.GetAsync(principal);

        if (order.CustomerId != customer.Id)
        {
            throw new ForbiddenException("This order belongs to another customer.");
        }
    }

    // Whoever did not press cancel gets told about it
    private async Task NotifyCancelledAsync(Order order, ClaimsPrincipal principal)
    {
        if (principal.IsInRole(AuthConstants.AdminRole))
        {
            if (order.Customer.AppUserId is int userId)
            {
                await _notificationService.NotifyUserAsync(
                    userId,
                    NotificationType.OrderCancelled,
                    $"Order #{order.Id} was cancelled",
                    "An administrator cancelled your order and the items went back to stock.",
                    $"/orders/{order.Id}");
            }

            return;
        }

        await _notificationService.NotifyAdminsAsync(
            NotificationType.OrderCancelled,
            $"Order #{order.Id} was cancelled",
            $"{order.Customer.Name} cancelled their order.",
            $"/admin/orders/{order.Id}");
    }

    // ========================================
    // Task 6 - Validation
    // ========================================

    private static void ValidateCreateOrderRequest(
        CreateOrderRequest request)
    {
        if (request.DiscountPercent < 0 ||
            request.DiscountPercent > 100)
        {
            throw new ArgumentException(
                "DiscountPercent must be between 0 and 100.");
        }

        if (request.Items == null ||
            request.Items.Count == 0)
        {
            throw new ArgumentException(
                "Order must contain at least one item.");
        }

        var invalidQuantityItem = request.Items
            .FirstOrDefault(item => item.Quantity < 1);

        if (invalidQuantityItem != null)
        {
            throw new ArgumentException(
                $"Quantity for product {invalidQuantityItem.ProductId} must be at least 1.");
        }

        // Check duplicate products

        var duplicateProduct = request.Items
            .GroupBy(item => item.ProductId)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateProduct != null)
        {
            throw new ArgumentException(
                $"Product {duplicateProduct.Key} cannot appear more than once.");
        }
    }

    // Make sure every product exists, is active and has enough stock
    private static void ValidateProductsAvailable(
        List<CreateOrderItemRequest> items,
        List<Product> products)
    {
        foreach (var item in items)
        {
            var product = products
                .FirstOrDefault(p =>
                    p.Id == item.ProductId);

            if (product == null)
            {
                throw new ArgumentException(
                    $"Product with id {item.ProductId} does not exist.");
            }

            if (!product.IsActive)
            {
                throw new ArgumentException(
                    $"Product {product.Name} is not active.");
            }

            if (item.Quantity > product.Stock)
            {
                throw new ArgumentException(
                    $"Quantity for product {product.Name} exceeds available stock. Available stock is {product.Stock}.");
            }
        }
    }

    // ========================================
    // Mapping
    // ========================================

    private static OrderResponse MapToOrderResponse(
        Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,

            CustomerId =
                order.CustomerId,

            CustomerName =
                order.Customer.Name,

            OrderDate =
                order.OrderDate,

            Subtotal =
                order.Subtotal,

            DiscountAmount =
                order.DiscountAmount,

            Total =
                order.Total,

            Status =
                order.Status,

            Items = order.OrderItems
                .OrderBy(item => item.Id)
                .Select(item =>
                    new OrderItemResponse
                    {
                        Id = item.Id,

                        ProductId =
                            item.ProductId,

                        ProductName =
                            item.Product.Name,

                        ProductImageUrl =
                            item.Product.ImageUrl,

                        Quantity =
                            item.Quantity,

                        UnitPrice =
                            item.UnitPrice,

                        LineTotal =
                            item.LineTotal
                    })
                .ToList()
        };
    }
}
