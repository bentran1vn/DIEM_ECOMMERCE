using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Feedback;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Queries.Feedback;

public class GetFeedbackByCustomerQueryHandler : IQueryHandler<Contract.Services.Feedback.Queries.GetFeedbackByCustomerQuery, PagedResult<Responses.FeedbackResponse>>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Feedbacks, Guid> _feedbackRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, OrderDetails, Guid> _orderDetailRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> _matchRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> _userRepository;

    public GetFeedbackByCustomerQueryHandler(
        IRepositoryBase<ApplicationReplicateDbContext, Feedbacks, Guid> feedbackRepository,
        IRepositoryBase<ApplicationReplicateDbContext, OrderDetails, Guid> orderDetailRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> matchRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> userRepository)
    {
        _feedbackRepository = feedbackRepository;
        _orderDetailRepository = orderDetailRepository;
        _matchRepository = matchRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResult<Responses.FeedbackResponse>>> Handle(
        Contract.Services.Feedback.Queries.GetFeedbackByCustomerQuery request, CancellationToken cancellationToken)
    {
        // Get customer for response
        var customer = await _userRepository.FindAll(
                u => u.CustomerId == request.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (customer == null)
        {
            return Result.Failure<PagedResult<Responses.FeedbackResponse>>(new Error("404", "Customer not found"));
        }

        // Get all feedback from this customer
        var feedbackQuery = _feedbackRepository.FindAll(
                f => f.CustomerId == request.CustomerId && !f.IsDeleted)
            .Include(f => f.Images)
            .OrderByDescending(f => f.CreatedOnUtc);

        // Execute query with paging
        var pagedFeedbacks = await PagedResult<Feedbacks>.CreateAsync(
            feedbackQuery,
            request.PageIndex,
            request.PageSize);

        if (pagedFeedbacks.Items.Count == 0)
        {
            // No feedback found for this customer, return empty result
            return Result.Success(
                PagedResult<Responses.FeedbackResponse>.Create(
                    new List<Responses.FeedbackResponse>(),
                    request.PageIndex,
                    request.PageSize,
                    0));
        }

        // Get all order detail IDs to load product information
        var orderDetailIds = pagedFeedbacks.Items
            .Select(f => f.OrderDetailId)
            .Distinct()
            .ToList();

        // Get order details with products
        var orderDetails = await _orderDetailRepository.FindAll(
                od => orderDetailIds.Contains(od.Id),
                od => od.Match)
            .ToDictionaryAsync(od => od.Id, od => od, cancellationToken);

        // Extract match IDs
        var matchIds = orderDetails.Values
            .Select(od => od.MatchId)
            .Distinct()
            .ToList();

        // Get match information
        var matches = await _matchRepository.FindAll(
                m => matchIds.Contains(m.Id))
            .Include(m => m.CoverImages)
            .ToDictionaryAsync(m => m.Id, m => m, cancellationToken);

        // Map feedback to response
        var feedbackResponses = pagedFeedbacks.Items.Select(feedback =>
        {
            // Get order detail for this feedback
            var orderDetail = orderDetails.GetValueOrDefault(feedback.OrderDetailId);
            if (orderDetail == null)
            {
                // Skip if order detail not found
                return null;
            }

            // Get match for this order detail
            var match = matches.GetValueOrDefault(orderDetail.MatchId);
            if (match == null)
            {
                // Skip if match not found
                return null;
            }

            return new Responses.FeedbackResponse
            {
                Id = feedback.Id,
                OrderDetailId = feedback.OrderDetailId,
                ProductName = match.Name,
                ProductImage = match.CoverImages.FirstOrDefault()?.Url ?? "",
                CustomerId = feedback.CustomerId,
                CustomerName = $"{customer.FirstName} {customer.LastName}",
                Rating = feedback.Rating,
                Comment = feedback.Comment,
                CreatedOnUtc = feedback.CreatedOnUtc,
                Images = feedback.Images.Select(i => new Responses.FeedbackMediaResponse
                {
                    Id = i.Id,
                    Url = i.Url
                }).ToList()
            };
        })
        .Where(f => f != null) // Filter out nulls from skipped items
        .ToList();

        // Create final paged result
        var result = PagedResult<Responses.FeedbackResponse>.Create(
            feedbackResponses,
            pagedFeedbacks.PageIndex,
            pagedFeedbacks.PageSize,
            pagedFeedbacks.TotalCount);

        return Result.Success(result);
    }
}