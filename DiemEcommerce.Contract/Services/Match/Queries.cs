using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;

namespace DiemEcommerce.Contract.Services.Match;

public class Queries
{
    public record GetAllMatchQuery(List<Guid>? CategoryId, string? SearchTerm, int PageIndex, int PageSize)
        : IQuery<PagedResult<Responses.MatchResponse>>;

    public record GetMatchByIdQuery(Guid Id) : IQuery<Responses.MatchDetailResponse>;
    
    public record GetMatchByFactoryIdQuery(Guid Id) : IQuery<PagedResult<Responses.MatchResponse>>;

}