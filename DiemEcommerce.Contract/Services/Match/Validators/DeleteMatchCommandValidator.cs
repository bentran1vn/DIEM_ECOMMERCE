using FluentValidation;

namespace DiemEcommerce.Contract.Services.Match.Validators;

public class DeleteMatchCommandValidator : AbstractValidator<Commands.DeleteMatchCommand>
{
    public DeleteMatchCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Match ID is required.");
        
        RuleFor(x => x.FactoryId)
            .NotEmpty().WithMessage("Factory ID is required.");
    }
}