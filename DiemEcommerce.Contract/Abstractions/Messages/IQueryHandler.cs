using DiemEcommerce.Contract.Abstractions.Shared;
using MediatR;

namespace DiemEcommerce.Contract.Abstractions.Messages;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}