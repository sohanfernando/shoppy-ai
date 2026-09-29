using AdvancedOrderSystem.Data;
using AdvancedOrderSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdvancedOrderSystem.Repositories.Impl;

public class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _context;

    public ReviewRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Review>> GetByProductAsync(int productId)
    {
        return await _context.Reviews
            .Include(r => r.Customer)
            .Include(r => r.Product)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Review>> GetAllAsync(int? productId, int? rating, int page, int pageSize)
    {
        return await Filter(productId, rating)
            .Include(r => r.Customer)
            .Include(r => r.Product)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(int? productId, int? rating)
    {
        return await Filter(productId, rating).CountAsync();
    }

    public async Task<Review?> GetByIdAsync(int id)
    {
        return await _context.Reviews
            .Include(r => r.Customer)
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Review?> GetByProductAndCustomerAsync(int productId, int customerId)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.CustomerId == customerId);
    }

    public async Task<bool> HasPurchasedAsync(int productId, int customerId)
    {
        return await _context.OrderItems
            .AnyAsync(item =>
                item.ProductId == productId &&
                item.Order.CustomerId == customerId &&
                item.Order.Status != OrderStatus.Cancelled);
    }

    public async Task AddAsync(Review review)
    {
        await _context.Reviews.AddAsync(review);
    }

    public void Remove(Review review)
    {
        _context.Reviews.Remove(review);
    }

    private IQueryable<Review> Filter(int? productId, int? rating)
    {
        var query = _context.Reviews.AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(r => r.ProductId == productId.Value);
        }

        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        return query;
    }
}
