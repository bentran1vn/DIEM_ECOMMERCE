using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Factory;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;

namespace DiemEcommerce.Application.UseCases.Queries.Factory;

public class GetFactoryByIdQueryHandler: IQueryHandler<Contract.Services.Factory.Queries.GetFactoryByIdQuery, Responses.FactoryResponse>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid> _factoryRepository;

    public GetFactoryByIdQueryHandler(IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid> factoryRepository)
    {
        _factoryRepository = factoryRepository;
    }

    public async Task<Result<Responses.FactoryResponse>> Handle(Contract.Services.Factory.Queries.GetFactoryByIdQuery request, CancellationToken cancellationToken)
    {
        var factories = await _factoryRepository.FindByIdAsync(request.Id, cancellationToken);
        
        if (factories == null)
            return Result.Failure<Responses.FactoryResponse>(new Error("404", "Factory not found"));
        
        var factoryResponse = new Responses.FactoryResponse(
            factories.Id, factories.Name, factories.Address, factories.PhoneNumber, factories.Email, factories.Website,
            factories.Description, factories.Logo, factories.TaxCode, factories.BankAccount, factories.BankName,
            null, factories.CreatedOnUtc
        );
        
        return Result.Success(factoryResponse);
    }
}