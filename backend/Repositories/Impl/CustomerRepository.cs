using AdvancedOrderSystem.Data;
using AdvancedOrderSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdvancedOrderSystem.Repositories.Impl;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        return await _context.Customers.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer?> GetByEmailAsync(string email)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
    }

    public async Task<Customer?> GetByAppUserIdAsync(int appUserId)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.AppUserId == appUserId);
    }

    public async Task AddAsync(Customer customer)
    {
        await _context.Customers.AddAsync(customer);
    }
}
