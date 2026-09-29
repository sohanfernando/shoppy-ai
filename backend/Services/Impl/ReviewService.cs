using System.Security.Claims;
using AdvancedOrderSystem.Auth;
using AdvancedOrderSystem.Data;
using AdvancedOrderSystem.Exceptions;
using AdvancedOrderSystem.Models.DTOs.Common;
using AdvancedOrderSystem.Models.DTOs.Review;
using AdvancedOrderSystem.Models.Entities;
using AdvancedOrderSystem.Repositories;

namespace AdvancedOrderSystem.Services.Impl;

public class ReviewService : IReviewService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IReviewRepository _reviewRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentCustomerService _currentCustomer;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public ReviewService(
        IReviewRepository reviewRepository,
        IProductRepository productRepository,
        ICurrentCustomerService currentCustomer,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _reviewRepository = reviewRepository;
        _productRepository = productRepository;
        _currentCustomer = currentCustomer;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductReviewsResponse> GetForProductAsync(
        int productId, ClaimsPrincipal principal)
    {
        var product = await _productRepository.GetByIdAsync(productId);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product with id {productId} was not found.");
        }

        var reviews = await _reviewRepository.GetByProductAsync(productId);

        var response = new ProductReviewsResponse
        {
            ReviewCount = reviews.Count,
            AverageRating = reviews.Count == 0
                ? 0
                : Math.Round(reviews.Average(review => review.Rating), 1),
            Reviews = reviews.Select(MapToResponse).ToList()
        };

        // Admins read reviews but never write them
        if (!principal.IsInRole(AuthConstants.CustomerRole))
        {
            response.CannotReviewReason = "Only customers can write reviews.";

            return response;
        }

        var customer = await _currentCustomer.GetAsync(principal);

        if (reviews.Any(review => review.CustomerId == customer.Id))
        {
            response.CannotReviewReason = "You have already reviewed this product.";
        }
        else if (!await _reviewRepository.HasPurchasedAsync(productId, customer.Id))
        {
            response.CannotReviewReason = "You can review this product after you order it.";
        }
        else
        {
            response.CanReview = true;
        }

        return response;
    }

    public async Task<ReviewResponse> CreateAsync(
        ClaimsPrincipal principal, CreateReviewRequest request)
    {
        if (request.Rating < Review.MinRating || request.Rating > Review.MaxRating)
        {
            throw new ArgumentException(
                $"Rating must be between {Review.MinRating} and {Review.MaxRating}.");
        }

        var comment = request.Comment?.Trim() ?? string.Empty;

        if (comment.Length == 0)
        {
            throw new ArgumentException("Please write a few words about the product.");
        }

        if (comment.Length > Review.CommentMaxLength)
        {
            throw new ArgumentException(
                $"Review cannot be longer than {Review.CommentMaxLength} characters.");
        }

        var product = await _productRepository.GetByIdAsync(request.ProductId);

        if (product == null)
        {
            throw new KeyNotFoundException(
                $"Product with id {request.ProductId} was not found.");
        }

        var customer = await _currentCustomer.GetAsync(principal);

        if (!await _reviewRepository.HasPurchasedAsync(product.Id, customer.Id))
        {
            throw new ForbiddenException("You can only review products you have ordered.");
        }

        var existing =
            await _reviewRepository.GetByProductAndCustomerAsync(product.Id, customer.Id);

        if (existing != null)
        {
            throw new ConflictException("You have already reviewed this product.");
        }

        var review = new Review
        {
            ProductId = product.Id,
            CustomerId = customer.Id,
            Rating = request.Rating,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };

        await _reviewRepository.AddAsync(review);
        await _unitOfWork.SaveChangesAsync();

        review.Product = product;
        review.Customer = customer;

        await _notificationService.NotifyAdminsAsync(
            NotificationType.ReviewPosted,
            "New product review",
            $"{customer.Name} rated {product.Name} {review.Rating}/{Review.MaxRating}.",
            "/admin/reviews");

        return MapToResponse(review);
    }

    public async Task<PagedResponse<ReviewResponse>> GetAllAsync(
        int? productId, int? rating, int page, int pageSize)
    {
        if (pageSize < 1)
        {
            pageSize = DefaultPageSize;
        }

        if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        page = Math.Clamp(page, 1, int.MaxValue / pageSize);

        var reviews = await _reviewRepository.GetAllAsync(productId, rating, page, pageSize);
        var totalItems = await _reviewRepository.CountAsync(productId, rating);

        return new PagedResponse<ReviewResponse>
        {
            Items = reviews.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    public async Task DeleteAsync(int id)
    {
        var review = await _reviewRepository.GetByIdAsync(id);

        if (review == null)
        {
            throw new KeyNotFoundException($"Review with id {id} was not found.");
        }

        _reviewRepository.Remove(review);

        await _unitOfWork.SaveChangesAsync();
    }

    private static ReviewResponse MapToResponse(Review review)
    {
        return new ReviewResponse
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ProductName = review.Product?.Name ?? string.Empty,
            CustomerId = review.CustomerId,
            CustomerName = review.Customer?.Name ?? string.Empty,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };
    }
}
