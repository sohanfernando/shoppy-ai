using System.Security.Claims;
using AdvancedOrderSystem.Models.DTOs.Common;
using AdvancedOrderSystem.Models.DTOs.Review;

namespace AdvancedOrderSystem.Services;

public interface IReviewService
{
    // What a customer sees on a product page, including whether they may review it
    Task<ProductReviewsResponse> GetForProductAsync(int productId, ClaimsPrincipal principal);

    Task<ReviewResponse> CreateAsync(ClaimsPrincipal principal, CreateReviewRequest request);

    Task<PagedResponse<ReviewResponse>> GetAllAsync(
        int? productId, int? rating, int page, int pageSize);

    Task DeleteAsync(int id);
}
