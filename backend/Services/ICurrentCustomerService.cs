using System.Security.Claims;
using AdvancedOrderSystem.Models.Entities;

namespace AdvancedOrderSystem.Services;

// Resolves the Customer record behind the signed-in account
public interface ICurrentCustomerService
{
    Task<Customer> GetAsync(ClaimsPrincipal principal);

    int GetUserId(ClaimsPrincipal principal);
}
