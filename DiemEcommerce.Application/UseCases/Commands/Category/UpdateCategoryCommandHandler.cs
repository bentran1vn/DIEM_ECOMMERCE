using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;

namespace DiemEcommerce.Application.UseCases.Commands.Category;

public class UpdateCategoryCommandHandler: ICommandHandler<Contract.Services.Category.Commands.UpdateCategoryCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Categories, Guid> _categoryRepository;

    public UpdateCategoryCommandHandler(IRepositoryBase<ApplicationDbContext, Categories, Guid> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result> Handle(Contract.Services.Category.Commands.UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var isExist = await _categoryRepository.FindByIdAsync(request.Id, cancellationToken);
        if (isExist == null)
        {
            return Result.Failure(new Error("400", "Parent category not found"));
        }
        
        var isExistName = await _categoryRepository.FindSingleAsync(
            x => x.Name.Trim().ToLower().Equals(request.Name.Trim().ToLower()) &&
                 !x.IsParent
            , cancellationToken);
        
        if (isExistName == null)
        {
            return Result.Failure(new Error("400", "Exist category name"));
        }
        
        isExist.Name = request.Name;
        isExist.Description = request.Description;
        
        if(request.ParentId != null)
        {
            var isExistParent = await _categoryRepository.FindByIdAsync(request.ParentId.Value, cancellationToken);
            if (isExistParent == null)
            {
                return Result.Failure(new Error("400", "Parent category not found"));
            }
            isExist.ParentId = request.ParentId;
        }
        
        return Result.Success("Update category successfully");
    }
}