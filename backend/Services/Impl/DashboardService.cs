using System.Security.Claims;
using AdvancedOrderSystem.Data;
using AdvancedOrderSystem.Models.DTOs.Dashboard;
using AdvancedOrderSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdvancedOrderSystem.Services.Impl;

public class DashboardService : IDashboardService
{
    private const int MonthsOfHistory = 6;
    private const int TopProductCount = 5;
    private const int RecentOrderCount = 5;

    // Must match LOW_STOCK_THRESHOLD in the frontend's product-list.ts
    private const int LowStockThreshold = 5;

    private readonly ApplicationDbContext _context;
    private readonly ICurrentCustomerService _currentCustomer;

    public DashboardService(
        ApplicationDbContext context,
        ICurrentCustomerService currentCustomer)
    {
        _context = context;
        _currentCustomer = currentCustomer;
    }

    public async Task<CustomerDashboardResponse> GetForCustomerAsync(ClaimsPrincipal principal)
    {
        var customer = await _currentCustomer.GetAsync(principal);

        var orders = await _context.Orders
            .Where(order => order.CustomerId == customer.Id)
            .Select(order => new
            {
                order.Id,
                order.OrderDate,
                order.Total,
                order.Subtotal,
                order.DiscountAmount,
                order.Status,
                ItemCount = order.OrderItems.Count
            })
            .ToListAsync();

        var paidOrders = orders
            .Where(order => order.Status != OrderStatus.Cancelled)
            .ToList();

        var totalPaid = paidOrders.Sum(order => order.Total);

        var response = new CustomerDashboardResponse
        {
            TotalOrders = orders.Count,
            ConfirmedOrders = paidOrders.Count,
            CancelledOrders = orders.Count - paidOrders.Count,
            TotalPaid = totalPaid,
            TotalSaved = paidOrders.Sum(order => order.DiscountAmount),
            AverageOrderValue = paidOrders.Count == 0
                ? 0
                : Math.Round(totalPaid / paidOrders.Count, 2),
            LastOrderDate = orders.Count == 0
                ? null
                : orders.Max(order => order.OrderDate),
            RecentOrders = orders
                .OrderByDescending(order => order.OrderDate)
                .ThenByDescending(order => order.Id)
                .Take(RecentOrderCount)
                .Select(order => new RecentOrderSummary
                {
                    Id = order.Id,
                    OrderDate = order.OrderDate,
                    Total = order.Total,
                    Status = order.Status,
                    ItemCount = order.ItemCount
                })
                .ToList()
        };

        response.MonthlySpend = BuildMonthlySpend(
            paidOrders.Select(order => (order.OrderDate, order.Total)));

        var purchasedItems = await _context.OrderItems
            .Where(item =>
                item.Order.CustomerId == customer.Id &&
                item.Order.Status != OrderStatus.Cancelled)
            .Select(item => new
            {
                item.ProductId,
                ProductName = item.Product.Name,
                item.Quantity,
                item.LineTotal
            })
            .ToListAsync();

        response.ItemsOrdered = purchasedItems.Sum(item => item.Quantity);

        response.SpendByProduct = purchasedItems
            .GroupBy(item => new { item.ProductId, item.ProductName })
            .Select(group => new ProductSpendSlice
            {
                ProductId = group.Key.ProductId,
                ProductName = group.Key.ProductName,
                Total = group.Sum(item => item.LineTotal),
                Quantity = group.Sum(item => item.Quantity)
            })
            .OrderByDescending(slice => slice.Total)
            .Take(TopProductCount)
            .ToList();

        return response;
    }

    public async Task<AdminDashboardResponse> GetForAdminAsync()
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var ordersThisMonth = await _context.Orders
            .Where(order =>
                order.Status != OrderStatus.Cancelled &&
                order.OrderDate >= startOfMonth)
            .Select(order => order.Total)
            .ToListAsync();

        var topSellingProducts = await _context.OrderItems
            .Where(item => item.Order.Status != OrderStatus.Cancelled)
            .GroupBy(item => new { item.ProductId, item.Product.Name })
            .Select(group => new TopSellingProduct
            {
                ProductId = group.Key.ProductId,
                ProductName = group.Key.Name,
                QuantitySold = group.Sum(item => item.Quantity),
                Revenue = group.Sum(item => item.LineTotal)
            })
            .OrderByDescending(product => product.Revenue)
            .Take(TopProductCount)
            .ToListAsync();

        var lowStockProducts = await _context.Products
            .Where(product => product.IsActive && product.Stock < LowStockThreshold)
            .OrderBy(product => product.Stock)
            .Select(product => new LowStockProduct
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Stock = product.Stock
            })
            .ToListAsync();

        return new AdminDashboardResponse
        {
            RevenueThisMonth = ordersThisMonth.Sum(),
            OrdersThisMonth = ordersThisMonth.Count,
            TopSellingProducts = topSellingProducts,
            LowStockProducts = lowStockProducts
        };
    }

    // Always returns the last six months, so the chart keeps a steady shape
    private static List<MonthlySpendPoint> BuildMonthlySpend(
        IEnumerable<(DateTime OrderDate, decimal Total)> orders)
    {
        var ordersByMonth = orders
            .GroupBy(order => new DateTime(order.OrderDate.Year, order.OrderDate.Month, 1))
            .ToDictionary(
                group => group.Key,
                group => (Total: group.Sum(order => order.Total), Count: group.Count()));

        var firstMonth = DateTime.UtcNow.Date;
        firstMonth = new DateTime(firstMonth.Year, firstMonth.Month, 1)
            .AddMonths(-(MonthsOfHistory - 1));

        return Enumerable
            .Range(0, MonthsOfHistory)
            .Select(offset =>
            {
                var month = firstMonth.AddMonths(offset);
                ordersByMonth.TryGetValue(month, out var totals);

                return new MonthlySpendPoint
                {
                    Month = month,
                    Total = totals.Total,
                    OrderCount = totals.Count
                };
            })
            .ToList();
    }
}
