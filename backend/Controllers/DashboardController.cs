using AdvancedOrderSystem.Auth;
using AdvancedOrderSystem.Models.DTOs.Dashboard;
using AdvancedOrderSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdvancedOrderSystem.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("customer")]
    [Authorize(Policy = AuthConstants.CustomerPolicy)]
    public async Task<ActionResult<CustomerDashboardResponse>> GetCustomerDashboard()
    {
        return Ok(await _dashboardService.GetForCustomerAsync(User));
    }
}
