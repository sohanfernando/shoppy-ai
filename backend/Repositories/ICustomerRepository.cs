using AdvancedOrderSystem.Models.Entities;

namespace AdvancedOrderSystem.Repositories;

public interface ICustomerRepository
{
    Task<List<Customer>> GetAllAsync();

    Task<Customer?> GetByIdAsync(int id);

    Task<Customer?> GetByEmailAsync(string email);

    // The customer record behind a sign-in account
    Task<Customer?> GetByAppUserIdAsync(int appUserId);

    Task AddAsync(Customer customer);
}
