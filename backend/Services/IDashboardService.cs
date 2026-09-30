using System.Security.Claims;
using AdvancedOrderSystem.Models.DTOs.Dashboard;

namespace AdvancedOrderSystem.Services;

public interface IDashboardService
{
    Task<CustomerDashboardResponse> GetForCustomerAsync(ClaimsPrincipal principal);

    Task<AdminDashboardResponse> GetForAdminAsync();
}
