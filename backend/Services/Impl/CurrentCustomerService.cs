using System.Security.Claims;
using AdvancedOrderSystem.Exceptions;
using AdvancedOrderSystem.Models.Entities;
using AdvancedOrderSystem.Repositories;

namespace AdvancedOrderSystem.Services.Impl;

public class CurrentCustomerService : ICurrentCustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CurrentCustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer> GetAsync(ClaimsPrincipal principal)
    {
        var userId = GetUserId(principal);

        var customer = await _customerRepository.GetByAppUserIdAsync(userId);

        if (customer == null)
        {
            throw new ForbiddenException("This account is not linked to a customer profile.");
        }

        return customer;
    }

    public int GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var userId))
        {
            throw new AuthenticationFailedException("Your session is no longer valid.");
        }

        return userId;
    }
}
