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
    private readonly IRepositoryBase<ApplicationDbContext, Customers, Guid> _customersRepository;
    private readonly IPasswordHasherService _passwordHasherService;

    public RegisterCommandHandler(IRepositoryBase<ApplicationDbContext, Users, Guid> userRepository, IPasswordHasherService passwordHasherService, IRepositoryBase<ApplicationDbContext, Customers, Guid> customersRepository)
    {
        _userRepository = userRepository;
        _passwordHasherService = passwordHasherService;
        _customersRepository = customersRepository;
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
            PhoneNumber = request.Phonenumber,
            Password = hashingPassword,
        };
        

        if (request.Role == 0)
        {
            user.RolesId = new Guid("5a900888-430b-4073-a2f4-824659ff36bf");
            
            var customer = new Customers()
            {
                Id = Guid.NewGuid(),
                CreatedOnUtc = DateTime.UtcNow,
            };
            
            user.CustomersId = customer.Id;
            
            _userRepository.Add(user);
            
            _customersRepository.Add(customer);
        }
        else if (request.Role == 1)
        {
            user.RolesId = new Guid("6a900888-430b-4073-a2f4-824659ff36bf");
            
            _userRepository.Add(user);
        }
        else
        {
            throw new Exception("Role not found !");
        }
        

        return Result.Success(user);
    }
}