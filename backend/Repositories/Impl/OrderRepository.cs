using AdvancedOrderSystem.Data;
using AdvancedOrderSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdvancedOrderSystem.Repositories.Impl;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task<Order?> GetByIdWithDetailsAsync(int id)
    {
        return await OrdersWithDetails().FirstOrDefaultAsync(order => order.Id == id);
    }

    public async Task LockCheckoutAsync(int customerId, Guid checkoutId)
    {
        var resource = $"checkout:{customerId}:{checkoutId:N}";
        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = {resource},
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @result < 0 THROW 50003, 'Checkout is busy. Please retry.', 1;
            """);
    }

    public Task<Order?> GetByCheckoutIdAsync(int customerId, Guid checkoutId)
    {
        return OrdersWithDetails().FirstOrDefaultAsync(
            order => order.CustomerId == customerId && order.CheckoutId == checkoutId);
    }

    private IQueryable<Order> OrdersWithDetails()
    {
        return _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product);
    }

    public async Task<List<Order>> GetAllAsync(
        string? status,
        int? customerId,
        int page,
        int pageSize
    )
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o =>
                o.Status == status);
        }

        if (customerId.HasValue)
        {
            query = query.Where(o =>
                o.CustomerId == customerId.Value);
        }

        return await query
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(
        string? status,
        int? customerId)
    {
        var query = _context.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o =>
                o.Status == status);
        }

        if (customerId.HasValue)
        {
            query = query.Where(o =>
                o.CustomerId == customerId.Value);
        }

        return await query.CountAsync();
    }

    public async Task<bool> TryCancelAsync(int id)
    {
        var affectedRows = await _context.Orders
            .Where(o =>
                o.Id == id &&
                o.Status != OrderStatus.Cancelled)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(o => o.Status, OrderStatus.Cancelled));

        return affectedRows == 1;
    }
}
