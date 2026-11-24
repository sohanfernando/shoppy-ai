using AdvancedOrderSystem.Models.Entities;

namespace AdvancedOrderSystem.Repositories;

public interface IOrderRepository
{
    Task AddAsync(Order order);

    Task<Order?> GetByIdWithDetailsAsync(int id);

    Task LockCheckoutAsync(int customerId, Guid checkoutId);

    Task<Order?> GetByCheckoutIdAsync(int customerId, Guid checkoutId);

    Task<List<Order>> GetAllAsync(
        string? status,
        int? customerId,
        int page,
        int pageSize
    );

    Task<int> CountAsync(
        string? status,
        int? customerId
    );

    // Atomically marks the order cancelled; returns false if it was already cancelled
    Task<bool> TryCancelAsync(int id);
}
