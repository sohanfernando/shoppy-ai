using AdvancedOrderSystem.Models.Entities;

namespace AdvancedOrderSystem.Repositories;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync(string? search, string? category);

    Task<Product?> GetByIdAsync(int id);

    Task<Product?> GetBySkuAsync(string sku);

    Task<List<Product>> GetByIdsAsync(List<int> ids);

    Task AddAsync(Product product);

    // Atomically reduces stock; returns false if the product is inactive or stock is insufficient
    Task<bool> TryDecreaseStockAsync(int productId, int quantity);

    Task IncreaseStockAsync(int productId, int quantity);
}
