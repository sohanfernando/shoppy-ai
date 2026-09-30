using AdvancedOrderSystem.Data;
using AdvancedOrderSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdvancedOrderSystem.Repositories.Impl;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllAsync(string? search, string? category)
    {
        var query = _context.Products
            .Where(p => p.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(p =>
                p.Name.Contains(search) ||
                p.SKU.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Product>> GetByIdsAsync(List<int> ids)
    {
        return await _context.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == sku);
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public async Task<bool> TryDecreaseStockAsync(int productId, int quantity)
    {
        var affectedRows = await _context.Products
            .Where(p =>
                p.Id == productId &&
                p.IsActive &&
                p.Stock >= quantity)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(p => p.Stock, p => p.Stock - quantity));

        return affectedRows == 1;
    }

    public async Task IncreaseStockAsync(int productId, int quantity)
    {
        await _context.Products
            .Where(p => p.Id == productId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(p => p.Stock, p => p.Stock + quantity));
    }
}
