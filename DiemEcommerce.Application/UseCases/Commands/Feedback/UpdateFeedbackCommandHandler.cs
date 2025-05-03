using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Feedback;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Commands.Feedback;

public class UpdateFeedbackCommandHandler : ICommandHandler<Contract.Services.Feedback.Commands.UpdateFeedbackCommand, Responses.FeedbackResponse>
{
    private readonly IRepositoryBase<ApplicationDbContext, Feedbacks, Guid> _feedbackRepository;
    private readonly IRepositoryBase<ApplicationDbContext, FeedbackMedias, Guid> _feedbackMediaRepository;
    private readonly IRepositoryBase<ApplicationDbContext, OrderDetails, Guid> _orderDetailRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Users, Guid> _userRepository;
    private readonly IMediaService _mediaService;

    public UpdateFeedbackCommandHandler(
        IRepositoryBase<ApplicationDbContext, Feedbacks, Guid> feedbackRepository,
        IRepositoryBase<ApplicationDbContext, FeedbackMedias, Guid> feedbackMediaRepository,
        IRepositoryBase<ApplicationDbContext, OrderDetails, Guid> orderDetailRepository,
        IRepositoryBase<ApplicationDbContext, Users, Guid> userRepository,
        IMediaService mediaService)
    {
        _feedbackRepository = feedbackRepository;
        _feedbackMediaRepository = feedbackMediaRepository;
        _orderDetailRepository = orderDetailRepository;
        _userRepository = userRepository;
        _mediaService = mediaService;
    }

    public async Task<Result<Responses.FeedbackResponse>> Handle(Contract.Services.Feedback.Commands.UpdateFeedbackCommand request, CancellationToken cancellationToken)
    {
        // Validate feedback exists and belongs to the customer
        var feedback = await _feedbackRepository.FindAll(
                f => f.Id == request.FeedbackId && f.CustomersId == request.CustomerId && !f.IsDeleted,
                f => f.OrderDetails,
                f => f.Images)
            .FirstOrDefaultAsync(cancellationToken);

        if (feedback == null)
        {
            return Result.Failure<Responses.FeedbackResponse>(new Error("404", "Feedback not found or you don't have permission to modify it"));
        }

        // Validate rating
        if (request.Rating < 1 || request.Rating > 5)
        {
            return Result.Failure<Responses.FeedbackResponse>(new Error("400", "Rating must be between 1 and 5"));
        }

        // Get order detail and product info for response
        var orderDetail = feedback.OrderDetails;
        if (orderDetail == null)
        {
            return Result.Failure<Responses.FeedbackResponse>(new Error("404", "Related order detail not found"));
        }

        // Load match information
        var match = await _orderDetailRepository.FindAll(
                od => od.Id == feedback.OrderDetailsId,
                od => od.Matches)
            .Select(od => od.Matches)
            .FirstOrDefaultAsync(cancellationToken);

        if (match == null)
        {
            return Result.Failure<Responses.FeedbackResponse>(new Error("404", "Related product not found"));
        }

        // Delete images if specified
        if (request.DeleteImages != null && request.DeleteImages.Any())
        {
            var mediaToDelete = await _feedbackMediaRepository
                .FindAll(m => m.FeedbacksId == feedback.Id && request.DeleteImages.Contains(m.Id))
                .ToListAsync(cancellationToken);

            if (mediaToDelete.Any())
            {
                _feedbackMediaRepository.RemoveMultiple(mediaToDelete);
            }
        }

        // Upload new images if provided
        if (request.NewImages != null && request.NewImages.Count > 0)
        {
            var newFeedbackMedias = new List<FeedbackMedias>();

            foreach (var image in request.NewImages)
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

                        newFeedbackMedias.Add(media);
                    }
                    catch (Exception)
                    {
                        // Log error but continue with other images
                        continue;
                    }
                }
            }

            if (newFeedbackMedias.Any())
            {
                _feedbackMediaRepository.AddRange(newFeedbackMedias);
            }
        }

        // Update feedback
        feedback.Rating = request.Rating;
        feedback.Comment = request.Comment;

        // Get customer for response
        var customer = await _userRepository.FindByIdAsync(feedback.CustomersId, cancellationToken);

        // Create updated feedback response
        var existingImages = feedback.Images
            .Where(i => request.DeleteImages == null || !request.DeleteImages.Contains(i.Id))
            .Select(i => new Responses.FeedbackMediaResponse
            {
                Id = i.Id,
                Url = i.Url
            })
            .ToList();

        var response = new Responses.FeedbackResponse
        {
            Id = feedback.Id,
            OrderDetailId = feedback.OrderDetailsId,
            ProductName = match.Name,
            ProductImage = match.CoverImages.FirstOrDefault()?.Url ?? "",
            CustomerId = feedback.CustomersId,
            CustomerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Anonymous",
            Rating = feedback.Rating,
            Comment = feedback.Comment,
            CreatedOnUtc = feedback.CreatedOnUtc,
            Images = existingImages
        };

        return Result.Success(response);
    }
}