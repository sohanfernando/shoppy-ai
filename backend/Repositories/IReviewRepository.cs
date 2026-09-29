using AdvancedOrderSystem.Models.Entities;

namespace AdvancedOrderSystem.Repositories;

public interface IReviewRepository
{
    Task<List<Review>> GetByProductAsync(int productId);

    Task<List<Review>> GetAllAsync(int? productId, int? rating, int page, int pageSize);

    Task<int> CountAsync(int? productId, int? rating);

    Task<Review?> GetByIdAsync(int id);

    Task<Review?> GetByProductAndCustomerAsync(int productId, int customerId);

    // True when the customer has a confirmed order containing this product
    Task<bool> HasPurchasedAsync(int productId, int customerId);

    Task AddAsync(Review review);

    void Remove(Review review);
}
