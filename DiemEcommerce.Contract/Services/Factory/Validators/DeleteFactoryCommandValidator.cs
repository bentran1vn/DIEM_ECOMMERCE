using FluentValidation;

namespace DiemEcommerce.Contract.Services.Factory.Validators;

public class DeleteFactoryCommandValidator : AbstractValidator<Commands.DeleteFactoryCommand>
{
    public DeleteFactoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Factory ID is required.");
    }
}