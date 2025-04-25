using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;

namespace DiemEcommerce.Application.UseCases.Commands.Factory;

public class UpdateFactoryCommandHandler : ICommandHandler<Contract.Services.Factory.Commands.UpdateFactoryCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Factories, Guid> _factoryRepository;
    private readonly IMediaService _mediaService;

    public UpdateFactoryCommandHandler(IRepositoryBase<ApplicationDbContext, Factories, Guid> factoryRepository, IMediaService mediaService)
    {
        _factoryRepository = factoryRepository;
        _mediaService = mediaService;
    }

    public async Task<Result> Handle(Contract.Services.Factory.Commands.UpdateFactoryCommand request, CancellationToken cancellationToken)
    {
        var isExist = await _factoryRepository.FindByIdAsync(request.Id, cancellationToken);
        
        if(isExist == null || isExist.IsDeleted)
            return Result.Failure(new Error("500", "Factory not found"));

        if (isExist.UserId != request.UserId)
            return Result.Failure(new Error("403", "You are not authorized to update this factory"));
        
        var isExistName = await _factoryRepository.FindSingleAsync(x => 
            x.Name.Trim().ToLower() == request.Name.Trim().ToLower() && x.Id != request.Id, cancellationToken);
        
        if(isExistName != null)
            return Result.Failure(new Error("500", "Factory with this name already exists"));
        
        isExist.Name = request.Name;
        isExist.Description = request.Description;
        isExist.BankAccount = request.BankAccount;
        isExist.Address = request.Address;
        isExist.PhoneNumber = request.PhoneNumber;
        isExist.Email = request.Email;
        isExist.Website = request.Website;
        isExist.BankName = request.BankName;
        isExist.UserId = request.UserId;
        isExist.TaxCode = request.TaxCode;
        
        if(request.Logo != null)
        {
            var logo = await _mediaService.UploadImageAsync(request.Logo);
            isExist.Logo = logo;
        }
        
        return Result.Success("Factory updated successfully");
    }
}