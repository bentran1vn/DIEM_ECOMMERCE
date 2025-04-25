using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;

namespace DiemEcommerce.Contract.Services.Factory;

public static class Queries
{
    public record GetAllFactoriesQuery(int PageIndex, int PageSize, string? SearchTerm) : IQuery<PagedResult<Responses.FactoryResponse>>;
    public record GetFactoryByIdQuery(Guid Id) : IQuery<Responses.FactoryResponse>;
}