using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;

namespace DiemEcommerce.Contract.Services.Feedback;

public static class Queries
{
    public record GetFeedbackByProductQuery(
        Guid MatchId,
        int PageIndex = 1,
        int PageSize = 10
    ) : IQuery<PagedResult<Responses.FeedbackResponse>>;
    
    public record GetFeedbackByCustomerQuery(
        Guid CustomerId,
        int PageIndex = 1,
        int PageSize = 10
    ) : IQuery<PagedResult<Responses.FeedbackResponse>>;
    
    public record GetFeedbackByIdQuery(
        Guid FeedbackId
    ) : IQuery<Responses.FeedbackResponse>;
}