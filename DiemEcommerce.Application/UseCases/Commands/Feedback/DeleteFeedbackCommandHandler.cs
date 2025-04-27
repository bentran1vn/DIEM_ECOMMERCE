using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Feedback;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Commands.Feedback;

public class DeleteFeedbackCommandHandler : ICommandHandler<Contract.Services.Feedback.Commands.DeleteFeedbackCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Feedbacks, Guid> _feedbackRepository;
    private readonly IRepositoryBase<ApplicationDbContext, FeedbackMedias, Guid> _feedbackMediaRepository;

    public DeleteFeedbackCommandHandler(
        IRepositoryBase<ApplicationDbContext, Feedbacks, Guid> feedbackRepository,
        IRepositoryBase<ApplicationDbContext, FeedbackMedias, Guid> feedbackMediaRepository)
    {
        _feedbackRepository = feedbackRepository;
        _feedbackMediaRepository = feedbackMediaRepository;
    }

    public async Task<Result> Handle(Contract.Services.Feedback.Commands.DeleteFeedbackCommand request, CancellationToken cancellationToken)
    {
        // Validate feedback exists and belongs to the customer
        var feedback = await _feedbackRepository.FindAll(
                f => f.Id == request.FeedbackId && f.CustomerId == request.CustomerId && !f.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (feedback == null)
        {
            return Result.Failure(new Error("404", "Feedback not found or you don't have permission to delete it"));
        }

        // Get all associated media for removal
        var feedbackMedia = await _feedbackMediaRepository.FindAll(
                fm => fm.FeedbackId == request.FeedbackId)
            .ToListAsync(cancellationToken);

        // Remove the feedback media first (one-to-many relationship)
        if (feedbackMedia.Any())
        {
            _feedbackMediaRepository.RemoveMultiple(feedbackMedia);
        }

        // Remove the feedback
        _feedbackRepository.Remove(feedback);

        return Result.Success();
    }
}