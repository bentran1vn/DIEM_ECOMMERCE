using FluentValidation;

namespace DiemEcommerce.Contract.Services.Match.Validators;

public class CreateMatchCommandValidator : AbstractValidator<Commands.CreateMatchCommand>
{
    public CreateMatchCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.CoverImages)
            .NotEmpty().WithMessage("At least one cover image is required.");
            
        RuleForEach(x => x.CoverImages)
            .NotEmpty().WithMessage("Cover image URL cannot be empty.");

        RuleFor(x => x.FactoryId)
            .NotEmpty().WithMessage("Factory ID is required.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");
    }
}