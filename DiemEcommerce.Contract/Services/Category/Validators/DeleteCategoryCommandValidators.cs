using FluentValidation;

namespace DiemEcommerce.Contract.Services.Category.Validators;

public class DeleteCategoryCommandValidators: AbstractValidator<Commands.DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidators()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}