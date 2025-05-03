using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;

namespace DiemEcommerce.Application.UseCases.Commands.Factory;

public class CreateFactoryCommandHandler : ICommandHandler<Contract.Services.Factory.Commands.CreateFactoryCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Factories, Guid> _factoryRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Users, Guid> _userRepository;
    private readonly IMediaService _mediaService;

    public CreateFactoryCommandHandler(IRepositoryBase<ApplicationDbContext, Factories, Guid> factoryRepository, IMediaService mediaService, IRepositoryBase<ApplicationDbContext, Users, Guid> userRepository)
    {
        _factoryRepository = factoryRepository;
        _mediaService = mediaService;
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(Contract.Services.Factory.Commands.CreateFactoryCommand request, CancellationToken cancellationToken)
    {
        var isExistName = await _factoryRepository.FindSingleAsync(x => 
            x.Name.Trim().ToLower() == request.Name.Trim().ToLower(), cancellationToken);
        
        if(isExistName != null)
            return Result.Failure(new Error("500", "Factory with this name already exists"));
        
        var user = await _userRepository.FindSingleAsync(x => 
            x.Id == request.UserId, cancellationToken);
        
        if (user == null)
        {
            return Result.Failure(new Error("500", "User not found"));
        }
        
        var logo = await _mediaService.UploadImageAsync(request.Logo);

        var factory = new Factories()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Logo = logo,
            BankAccount = request.BankAccount,
            Address = request.Address,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Website = request.Website,
            BankName = request.BankName,
            TaxCode = request.TaxCode,
        };
        
        _factoryRepository.Add(factory);
        
        // Update user with factory id
        user.FactoriesId = factory.Id;

        return Result.Success("Factory created successfully");
    }
}