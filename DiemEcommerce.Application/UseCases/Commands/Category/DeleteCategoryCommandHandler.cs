using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;

namespace DiemEcommerce.Application.UseCases.Commands.Category;

public class DeleteCategoryCommandHandler: ICommandHandler<Contract.Services.Category.Commands.DeleteCategoryCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Categories, Guid> _categoryRepository;

    public DeleteCategoryCommandHandler(IRepositoryBase<ApplicationDbContext, Categories, Guid> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result> Handle(Contract.Services.Category.Commands.DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var isExist = await _categoryRepository.FindByIdAsync(request.Id, cancellationToken);
        if (isExist == null || isExist.IsDeleted)
        {
            return Result.Failure(new Error("404", "Category not found"));
        }
        
        _categoryRepository.Remove(isExist);

        return Result.Success("Delete category successfully");
    }
}