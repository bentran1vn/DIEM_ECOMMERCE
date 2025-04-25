using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;

namespace DiemEcommerce.Application.UseCases.Commands.Factory;

public class DeleteFactoryCommandHandler : ICommandHandler<Contract.Services.Factory.Commands.DeleteFactoryCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Factories, Guid> _factoryRepository;

    public DeleteFactoryCommandHandler(IRepositoryBase<ApplicationDbContext, Factories, Guid> factoryRepository)
    {
        _factoryRepository = factoryRepository;
    }

    public async Task<Result> Handle(Contract.Services.Factory.Commands.DeleteFactoryCommand request, CancellationToken cancellationToken)
    {
        var isExist = await _factoryRepository.FindByIdAsync(request.Id, cancellationToken);
        
        if(isExist == null || isExist.IsDeleted)
            return Result.Failure(new Error("404", "Factory not found"));

        if (isExist.UserId != request.UserId)
            return Result.Failure(new Error("403", "You are not authorized to delete this factory"));
        
        return Result.Success("Factory deleted successfully");
    }
}