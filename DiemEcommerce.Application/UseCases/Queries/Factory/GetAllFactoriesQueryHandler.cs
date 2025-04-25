using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Factory;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiemEcommerce.Application.UseCases.Queries.Factory;

public class GetAllFactoriesQueryHandler: IQueryHandler<Contract.Services.Factory.Queries.GetAllFactoriesQuery, PagedResult<Responses.FactoryResponse>>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid> _factoryRepository;

    public GetAllFactoriesQueryHandler(IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid> factoryRepository)
    {
        _factoryRepository = factoryRepository;
    }

    public async Task<Result<PagedResult<Responses.FactoryResponse>>> Handle(Contract.Services.Factory.Queries.GetAllFactoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _factoryRepository.FindAll(x => !x.IsDeleted);
        
        query = request.SearchTerm != null
            ? query.Where(x => x.Name.Trim().ToLower().Contains(request.SearchTerm.Trim().ToLower()) ||
                               x.Description.Trim().ToLower().Contains(request.SearchTerm.Trim().ToLower()))
            : query;

        var selectQuery = query.Select(x => 
            new Responses.FactoryResponse(
                x.Id, x.Name, x.Address, x.PhoneNumber, x.Email, x.Website, x.Description,
                x.Logo, x.TaxCode, x.BankAccount, x.BankName, null, x.CreatedOnUtc
            )
        );
        
        var paging = await PagedResult<Responses.FactoryResponse>.CreateAsync(selectQuery, request.PageIndex, request.PageSize);
        
        // var list = paging.Items.Select()
        //
        // var result = PagedResult<Responses.FactoryResponse>.Create(, paging.PageIndex, paging.PageSize, paging.TotalCount); 

        return Result.Success(paging);
    }
}