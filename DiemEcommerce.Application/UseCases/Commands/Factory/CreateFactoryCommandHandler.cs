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
    private readonly IMediaService _mediaService;

    public CreateFactoryCommandHandler(IRepositoryBase<ApplicationDbContext, Factories, Guid> factoryRepository, IMediaService mediaService)
    {
        _factoryRepository = factoryRepository;
        _mediaService = mediaService;
    }

    public async Task<Result> Handle(Contract.Services.Factory.Commands.CreateFactoryCommand request, CancellationToken cancellationToken)
    {
        var isExistName = await _factoryRepository.FindSingleAsync(x => 
            x.Name.Trim().ToLower() == request.Name.Trim().ToLower(), cancellationToken);
        
        if(isExistName != null)
            return Result.Failure(new Error("500", "Factory with this name already exists"));
        
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
            UserId = request.UserId,
            TaxCode = request.TaxCode,
        };
        
        _factoryRepository.Add(factory);

        return Result.Success("Factory created successfully");
    }
}