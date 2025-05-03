using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Feedback;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Commands.Feedback;

public class CreateFeedbackCommandHandler : ICommandHandler<Contract.Services.Feedback.Commands.CreateFeedbackCommand, Responses.FeedbackResponse>
{
    private readonly IRepositoryBase<ApplicationDbContext, Feedbacks, Guid> _feedbackRepository;
    private readonly IRepositoryBase<ApplicationDbContext, FeedbackMedias, Guid> _feedbackMediaRepository;
    private readonly IRepositoryBase<ApplicationDbContext, OrderDetails, Guid> _orderDetailRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Orders, Guid> _orderRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Users, Guid> _userRepository;
    private readonly IMediaService _mediaService;

    public CreateFeedbackCommandHandler(
        IRepositoryBase<ApplicationDbContext, Feedbacks, Guid> feedbackRepository,
        IRepositoryBase<ApplicationDbContext, FeedbackMedias, Guid> feedbackMediaRepository,
        IRepositoryBase<ApplicationDbContext, OrderDetails, Guid> orderDetailRepository,
        IRepositoryBase<ApplicationDbContext, Orders, Guid> orderRepository,
        IRepositoryBase<ApplicationDbContext, Users, Guid> userRepository,
        IMediaService mediaService)
    {
        _feedbackRepository = feedbackRepository;
        _feedbackMediaRepository = feedbackMediaRepository;
        _orderDetailRepository = orderDetailRepository;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _mediaService = mediaService;
    }

    public async Task<Result<Responses.FeedbackResponse>> Handle(Contract.Services.Feedback.Commands.CreateFeedbackCommand request, CancellationToken cancellationToken)
    {
        // Validate order detail exists
        var orderDetail = await _orderDetailRepository.FindAll(
                od => od.Id == request.OrderDetailId,
                od => od.Matches,
                od => od.Orders)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (orderDetail == null)
        {
            return Result.Failure<Responses.FeedbackResponse>(new Error("404", "Order detail not found"));
        }

        // Validate customer owns the order
        if (orderDetail.Orders.CustomersId != request.CustomerId)
        {
            return Result.Failure<Responses.FeedbackResponse>(new Error("403", "You are not authorized to leave feedback for this order"));
        }

        // Validate order is delivered
        // if (orderDetail.Orders.Status != 4) // Delivered
        // {
        //     return Result.Failure<Responses.FeedbackResponse>(new Error("400", "Cannot leave feedback for orders that have not been delivered"));
        // }

        // Check if feedback already exists for this order detail
        var existingFeedback = await _feedbackRepository.FindAll(
                f => f.OrderDetailsId == request.OrderDetailId)
            .AnyAsync(cancellationToken);
        
        if (existingFeedback)
        {
            return Result.Failure<Responses.FeedbackResponse>(new Error("409", "Feedback already exists for this order detail"));
        }

        // Validate rating
        if (request.Rating < 1 || request.Rating > 5)
        {
            return Result.Failure<Responses.FeedbackResponse>(new Error("400", "Rating must be between 1 and 5"));
        }

        // Create feedback
        var feedback = new Feedbacks
        {
            Id = Guid.NewGuid(),
            OrderDetailsId = request.OrderDetailId,
            CustomersId = request.CustomerId,
            Rating = request.Rating,
            Comment = request.Comment,
        };

        _feedbackRepository.Add(feedback);

        // Upload images if provided
        if (request.Images != null && request.Images.Count > 0)
        {
            var feedbackMedias = new List<FeedbackMedias>();
            
            foreach (var image in request.Images)
            {
                if (image.Length > 0)
                {
                    try
                    {
                        var imageUrl = await _mediaService.UploadImageAsync(image);
                    
                        var media = new FeedbackMedias
                        {
                            Id = Guid.NewGuid(),
                            FeedbacksId = feedback.Id,
                            Url = imageUrl
                        };
                    
                        feedbackMedias.Add(media);
                    }
                    catch (Exception)
                    {
                        // Log error but continue with other images
                        continue;
                    }
                }
            }
            
            if (feedbackMedias.Any())
            {
                _feedbackMediaRepository.AddRange(feedbackMedias);
            }
        }

        // Get customer name
        var customer = await _userRepository.FindAll(u => u.CustomersId == request.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        // Map to response
        var response = new Responses.FeedbackResponse
        {
            Id = feedback.Id,
            OrderDetailId = feedback.OrderDetailsId,
            ProductName = orderDetail.Matches.Name,
            ProductImage = orderDetail.Matches.CoverImages.FirstOrDefault()?.Url ?? "",
            CustomerId = feedback.CustomersId,
            CustomerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Anonymous",
            Rating = feedback.Rating,
            Comment = feedback.Comment,
            CreatedOnUtc = feedback.CreatedOnUtc,
            Images = new List<Responses.FeedbackMediaResponse>() // Will be filled after save
        };

        return Result.Success(response);
    }
}