using DiemEcommerce.Contract.Abstractions.Shared;
using MediatR;

namespace DiemEcommerce.Contract.Abstractions.Messages;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
