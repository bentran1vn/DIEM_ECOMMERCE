using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;

namespace DiemEcommerce.Application.UseCases.Commands.Category;

public class CreateCategoryCommandHandler: ICommandHandler<Contract.Services.Category.Commands.CreateCategoryCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Categories, Guid> _categoryRepository;

    public CreateCategoryCommandHandler(IRepositoryBase<ApplicationDbContext, Categories, Guid> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result> Handle(Contract.Services.Category.Commands.CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var isExistName = await _categoryRepository.FindSingleAsync(
            x => x.Name.Trim().ToLower().Equals(request.Name.Trim().ToLower()) &&
                 !x.IsParent
            , cancellationToken);
        
        if (isExistName != null)
        {
            return Result.Failure(new Error("400", "Exist category name"));
        }
        
        if(request.ParentId != null)
        {
            var isExist = await _categoryRepository.FindByIdAsync(request.ParentId.Value, cancellationToken);
            if (isExist == null)
            {
                return Result.Failure(new Error("400", "Parent category not found"));
            }
        }
        
        var category = new Categories()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            ParentId = request.ParentId,
            IsParent = request.ParentId == null ? true : false,
        };
        
        _categoryRepository.Add(category);

        return Result.Success("Create category successfully");
    }
}