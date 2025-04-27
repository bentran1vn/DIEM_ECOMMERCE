using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Feedback;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Queries.Feedback;

public class GetFeedbackByIdQueryHandler : IQueryHandler<Contract.Services.Feedback.Queries.GetFeedbackByIdQuery, Responses.FeedbackResponse>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Feedbacks, Guid> _feedbackRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, OrderDetails, Guid> _orderDetailRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> _userRepository;

    public GetFeedbackByIdQueryHandler(
        IRepositoryBase<ApplicationReplicateDbContext, Feedbacks, Guid> feedbackRepository,
        IRepositoryBase<ApplicationReplicateDbContext, OrderDetails, Guid> orderDetailRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> userRepository)
    {
        _feedbackRepository = feedbackRepository;
        _orderDetailRepository = orderDetailRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<Responses.FeedbackResponse>> Handle(
        Contract.Services.Feedback.Queries.GetFeedbackByIdQuery request, CancellationToken cancellationToken)
    {
        // Get feedback with images
        var feedback = await _feedbackRepository.FindAll(
                f => f.Id == request.FeedbackId && !f.IsDeleted,
                f => f.Images)
            .FirstOrDefaultAsync(cancellationToken);

        if (feedback == null)
        {
            return Result.Failure<Responses.FeedbackResponse>(new Error("404", "Feedback not found"));
        }

        // Get order detail with product information
        var orderDetail = await _orderDetailRepository.FindAll(
                od => od.Id == feedback.OrderDetailId,
                od => od.Match)
            .FirstOrDefaultAsync(cancellationToken);

        if (orderDetail == null)
        {
            return Result.Failure<Responses.FeedbackResponse>(new Error("404", "Order detail not found"));
        }

        // Get customer information
        var customer = await _userRepository.FindByIdAsync(feedback.CustomerId, cancellationToken);
        
        if (customer == null)
        {
            // Proceed with anonymous customer if not found
            customer = new Users
            {
                FirstName = "Anonymous",
                LastName = "User"
            };
        }

        // Get product image if available
        var productImage = orderDetail.Match.CoverImages.FirstOrDefault()?.Url ?? "";

        // Create feedback response
        var response = new Responses.FeedbackResponse
        {
            Id = feedback.Id,
            OrderDetailId = feedback.OrderDetailId,
            ProductName = orderDetail.Match.Name,
            ProductImage = productImage,
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

        return Result.Success(response);
    }
}