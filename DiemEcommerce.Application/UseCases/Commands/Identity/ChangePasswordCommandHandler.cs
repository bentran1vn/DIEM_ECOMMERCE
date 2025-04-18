using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Identity;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;

namespace DiemEcommerce.Application.UseCases.Commands.Identity;

public class ChangePasswordCommandHandler : ICommandHandler<Command.ChangePasswordCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Users, Guid> _userRepository;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly ICacheService _cacheService;

    public ChangePasswordCommandHandler( IPasswordHasherService passwordHasherService, ICacheService cacheService, IRepositoryBase<ApplicationDbContext, Users, Guid> userRepository)
    {
        _passwordHasherService = passwordHasherService;
        _cacheService = cacheService;
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(Command.ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user =
            await _userRepository.FindSingleAsync(x =>
                x.Email.Equals(request.Email), cancellationToken);
        
        if (user is null)
        {
            throw new Exception("User Not Existed !");
        }
        
        var hashingPassword = _passwordHasherService.HashPassword(request.NewPassword);

        user.Password = hashingPassword;
        
        await _cacheService.RemoveAsync($"{nameof(Query.Login)}-UserAccount:{user.Email}", cancellationToken);

        return Result.Success("Change Password Successfully !");
    }
}