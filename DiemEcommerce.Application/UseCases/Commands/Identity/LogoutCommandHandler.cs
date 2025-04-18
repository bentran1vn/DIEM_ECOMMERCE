using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Identity;

namespace DiemEcommerce.Application.UseCases.Commands.Identity;

public class LogoutCommandHandler : ICommandHandler<Command.LogoutCommand>
{
    private readonly ICacheService _cacheService;

    public LogoutCommandHandler(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(Command.LogoutCommand request, CancellationToken cancellationToken)
    {
        await _cacheService.RemoveAsync($"{nameof(Query.Login)}-UserAccount:{request.UserAccount}", cancellationToken);
        return Result.Success("Logout Successfully");
    }
}