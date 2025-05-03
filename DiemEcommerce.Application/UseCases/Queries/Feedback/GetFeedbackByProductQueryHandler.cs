using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Feedback;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Queries.Feedback;

public class GetFeedbackByProductQueryHandler : IQueryHandler<Contract.Services.Feedback.Queries.GetFeedbackByProductQuery, PagedResult<Responses.FeedbackResponse>>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, OrderDetails, Guid> _orderDetailRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Feedbacks, Guid> _feedbackRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> _matchRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> _userRepository;

    public GetFeedbackByProductQueryHandler(
        IRepositoryBase<ApplicationReplicateDbContext, OrderDetails, Guid> orderDetailRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Feedbacks, Guid> feedbackRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> matchRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> userRepository)
    {
        _orderDetailRepository = orderDetailRepository;
        _feedbackRepository = feedbackRepository;
        _matchRepository = matchRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResult<Responses.FeedbackResponse>>> Handle(
        Contract.Services.Feedback.Queries.GetFeedbackByProductQuery request, CancellationToken cancellationToken)
    {
        // Validate product exists
        var match = await _matchRepository.FindByIdAsync(request.MatchId, cancellationToken);
        if (match == null || match.IsDeleted)
        {
            return Result.Failure<PagedResult<Responses.FeedbackResponse>>(new Error("404", "Product not found"));
        }

        // Get all order details containing this product
        var orderDetailIds = await _orderDetailRepository.FindAll(
                od => od.MatchesId == request.MatchId)
            .Select(od => od.Id)
            .ToListAsync(cancellationToken);

        if (orderDetailIds.Count == 0)
        {
            // No orders found for this product, return empty result
            return Result.Success(
                PagedResult<Responses.FeedbackResponse>.Create(
                    new List<Responses.FeedbackResponse>(),
                    request.PageIndex,
                    request.PageSize,
                    0));
        }

        // Get all feedback for these order details
        var feedbackQuery = _feedbackRepository.FindAll(
                f => orderDetailIds.Contains(f.OrderDetailsId) && !f.IsDeleted)
            .Include(f => f.Images)
            .OrderByDescending(f => f.CreatedOnUtc);

        // Execute query with paging
        var pagedFeedbacks = await PagedResult<Feedbacks>.CreateAsync(
            feedbackQuery,
            request.PageIndex,
            request.PageSize);

        // Get all customer IDs to load in a single query
        var customerIds = pagedFeedbacks.Items
            .Select(f => f.CustomersId)
            .Distinct()
            .ToList();

        // Get customer details in a single query
        var customers = await _userRepository.FindAll(
                u => customerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u, cancellationToken);

        // Map feedback to response
        var feedbackResponses = pagedFeedbacks.Items.Select(feedback =>
        {
            // Try to get customer name
            var customerName = "Anonymous";
            if (customers.TryGetValue(feedback.CustomersId, out var customer))
            {
                customerName = $"{customer.FirstName} {customer.LastName}";
            }

            return new Responses.FeedbackResponse
            {
                Id = feedback.Id,
                OrderDetailId = feedback.OrderDetailsId,
                ProductName = match.Name,
                ProductImage = match.CoverImages.FirstOrDefault()?.Url ?? "",
                CustomerId = feedback.CustomersId,
                CustomerName = customerName,
                Rating = feedback.Rating,
                Comment = feedback.Comment,
                CreatedOnUtc = feedback.CreatedOnUtc,
                Images = feedback.Images.Select(i => new Responses.FeedbackMediaResponse
                {
                    Id = i.Id,
                    Url = i.Url
                }).ToList()
            };
        }).ToList();

        // Create final paged result
        var result = PagedResult<Responses.FeedbackResponse>.Create(
            feedbackResponses,
            pagedFeedbacks.PageIndex,
            pagedFeedbacks.PageSize,
            pagedFeedbacks.TotalCount);

        return Result.Success(result);
    }
}