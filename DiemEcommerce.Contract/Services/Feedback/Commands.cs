using DiemEcommerce.Contract.Abstractions.Messages;
using Microsoft.AspNetCore.Http;

namespace DiemEcommerce.Contract.Services.Feedback;

public static class Commands
{
    public record CreateFeedbackCommand(
        Guid OrderDetailId,
        Guid CustomerId,
        int Rating,
        string Comment,
        IFormFileCollection? Images = null
    ) : ICommand<Responses.FeedbackResponse>;
    
    public record UpdateFeedbackCommand(
        Guid FeedbackId,
        Guid CustomerId,
        int Rating,
        string Comment,
        IFormFileCollection? NewImages = null,
        ICollection<Guid>? DeleteImages = null
    ) : ICommand<Responses.FeedbackResponse>;
    
    public record DeleteFeedbackCommand(
        Guid FeedbackId,
        Guid CustomerId
    ) : ICommand;
}