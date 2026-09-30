using AdvancedOrderSystem.Models.DTOs.Product;

namespace AdvancedOrderSystem.Services;

public interface IProductService
{
    Task<List<ProductResponse>> GetAllProductsAsync(string? search, string? category);

    Task<ProductResponse?> GetProductByIdAsync(int id);

    Task<ProductResponse> CreateProductAsync(CreateProductRequest request);
}