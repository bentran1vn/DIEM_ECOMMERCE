using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Identity;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;

namespace DiemEcommerce.Application.UseCases.Commands.Identity;

public class RegisterCommandHandler : ICommandHandler<Command.RegisterCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Users, Guid> _userRepository;
    private readonly IPasswordHasherService _passwordHasherService;

    public RegisterCommandHandler(IRepositoryBase<ApplicationDbContext, Users, Guid> userRepository, IPasswordHasherService passwordHasherService)
    {
        _userRepository = userRepository;
        _passwordHasherService = passwordHasherService;
    }

    public async Task<Result> Handle(Command.RegisterCommand request, CancellationToken cancellationToken)
    {
        var userExisted =
            await _userRepository.FindSingleAsync(x =>
                x.Email.Equals(request.Email) || x.Username.Equals(request.Username), cancellationToken);
        
        if (userExisted is not null)
        {
            throw new Exception("User Existed !");
        }

        var hashingPassword = _passwordHasherService.HashPassword(request.Password);
        
        var user = new Users()
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Username = request.Username,
            FirstName = request.FirstName,
            LastName = request.LastName,
            RoleId = Guid.NewGuid(),
            PhoneNumber = request.Phonenumber,
            Password = hashingPassword,
        };
        
        _userRepository.Add(user);

        return Result.Success(user);
    }
}