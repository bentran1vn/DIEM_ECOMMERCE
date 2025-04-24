using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Category;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Queries.Category;

public class GetAllCategoriesQueryHandler: IQueryHandler<Contract.Services.Category.Queries.GetAllCategoriesQuery, List<Responses.CategoryResponse>>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Categories, Guid> _categoryRepository;

    public GetAllCategoriesQueryHandler(IRepositoryBase<ApplicationReplicateDbContext, Categories, Guid> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<List<Responses.CategoryResponse>>> Handle(Contract.Services.Category.Queries.GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.FindAll(x => x.IsDeleted == false)
            .Select(x => new Responses.CategoryResponse(x.Id, x.Name, x.Description, x.ParentId, x.IsParent)
            ).ToListAsync(cancellationToken);
        
        return Result.Success(categories);
    }
}