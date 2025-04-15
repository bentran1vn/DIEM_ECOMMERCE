using DiemEcommerce.Contract.Abstractions.Shared;
using MediatR;

namespace DiemEcommerce.Contract.Abstractions.Messages;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}