using AdvancedOrderSystem.Auth;
using AdvancedOrderSystem.Models.DTOs.Common;
using AdvancedOrderSystem.Models.DTOs.Review;
using AdvancedOrderSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdvancedOrderSystem.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    // Shown on the product page; admins can read them too
    [HttpGet("product/{productId:int}")]
    [Authorize(Policy = AuthConstants.SignedInPolicy)]
    public async Task<ActionResult<ProductReviewsResponse>> GetForProduct(int productId)
    {
        return Ok(await _reviewService.GetForProductAsync(productId, User));
    }

    [HttpPost]
    [Authorize(Policy = AuthConstants.CustomerPolicy)]
    public async Task<ActionResult<ReviewResponse>> Create(CreateReviewRequest request)
    {
        return Ok(await _reviewService.CreateAsync(User, request));
    }

    // Admin reviews page
    [HttpGet]
    [Authorize(Policy = AuthConstants.AdminPolicy)]
    public async Task<ActionResult<PagedResponse<ReviewResponse>>> GetAll(
        [FromQuery] int? productId,
        [FromQuery] int? rating,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        return Ok(await _reviewService.GetAllAsync(productId, rating, page, pageSize));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthConstants.AdminPolicy)]
    public async Task<IActionResult> Delete(int id)
    {
        await _reviewService.DeleteAsync(id);

        return NoContent();
    }
}
