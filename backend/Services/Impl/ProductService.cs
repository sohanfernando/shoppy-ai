using AdvancedOrderSystem.Data;
using AdvancedOrderSystem.Models.DTOs.Product;
using AdvancedOrderSystem.Models.Entities;
using AdvancedOrderSystem.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AdvancedOrderSystem.Services.Impl;

public class ProductService : IProductService
{
    // Must match the column lengths in ApplicationDbContext
    private const int NameMaxLength = 150;
    private const int SkuMaxLength = 50;
    private const int ImageUrlMaxLength = 500;

    // SQL Server error numbers for unique index / constraint violations
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProductResponse>>
        GetAllProductsAsync(string? search, string? category)
    {
        var products =
            await _productRepository.GetAllAsync(search, category);

        return products
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<ProductResponse?>
        GetProductByIdAsync(int id)
    {
        var product =
            await _productRepository.GetByIdAsync(id);

        // Inactive products are hidden, matching the product list
        if (product == null || !product.IsActive)
        {
            return null;
        }

        return MapToResponse(product);
    }

    public async Task<ProductResponse>
        CreateProductAsync(CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Name is required."
            );
        }

        if (string.IsNullOrWhiteSpace(request.SKU))
        {
            throw new ArgumentException(
                "SKU is required."
            );
        }

        var name = request.Name.Trim();

        var normalizedSku =
            request.SKU.Trim().ToUpper();

        var imageUrl = string.IsNullOrWhiteSpace(request.ImageUrl)
            ? null
            : request.ImageUrl.Trim();

        if (name.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"Name cannot be longer than {NameMaxLength} characters."
            );
        }

        if (normalizedSku.Length > SkuMaxLength)
        {
            throw new ArgumentException(
                $"SKU cannot be longer than {SkuMaxLength} characters."
            );
        }

        if (!ProductCategory.All.Contains(request.Category))
        {
            throw new ArgumentException(
                $"Category must be one of: {string.Join(", ", ProductCategory.All)}."
            );
        }

        if (imageUrl != null && imageUrl.Length > ImageUrlMaxLength)
        {
            throw new ArgumentException(
                $"ImageUrl cannot be longer than {ImageUrlMaxLength} characters."
            );
        }

        if (request.UnitPrice <= 0)
        {
            throw new ArgumentException(
                "UnitPrice must be greater than 0."
            );
        }

        if (request.Stock < 0)
        {
            throw new ArgumentException(
                "Stock cannot be negative."
            );
        }

        var existingProduct =
            await _productRepository
                .GetBySkuAsync(normalizedSku);

        if (existingProduct != null)
        {
            throw new ArgumentException(
                "SKU already exists."
            );
        }

        var product = new Product
        {
            Name = name,
            SKU = normalizedSku,
            Category = request.Category,
            ImageUrl = imageUrl,
            UnitPrice = request.UnitPrice,
            Stock = request.Stock,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _productRepository.AddAsync(product);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        // Another request inserted the same SKU after the check above
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException
            {
                Number: UniqueIndexViolation or UniqueConstraintViolation
            })
        {
            throw new ArgumentException(
                "SKU already exists."
            );
        }

        return MapToResponse(product);
    }

    private static ProductResponse MapToResponse(
        Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Category = product.Category,
            ImageUrl = product.ImageUrl,
            UnitPrice = product.UnitPrice,
            Stock = product.Stock,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt
        };
    }
}
