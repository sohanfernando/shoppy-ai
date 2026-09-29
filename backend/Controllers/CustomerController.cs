using AdvancedOrderSystem.Auth;
using AdvancedOrderSystem.Models.DTOs.Customer;
using AdvancedOrderSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdvancedOrderSystem.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Policy = AuthConstants.AdminPolicy)]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerResponse>>> GetAllCustomers()
    {
        var customers = await _customerService.GetAllCustomerAsync();

        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerResponse>> GetCustomerById(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);

        if (customer == null)
        {
            return NotFound(new
            {
                message = $"Customer with id {id} was not found."
            });
        }

        return Ok(customer);
    }
}