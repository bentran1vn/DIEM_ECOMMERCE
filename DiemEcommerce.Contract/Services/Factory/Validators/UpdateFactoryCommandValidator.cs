using FluentValidation;

namespace DiemEcommerce.Contract.Services.Factory.Validators;

public class UpdateFactoryCommandValidator : AbstractValidator<Commands.UpdateFactoryCommand>
{
    public UpdateFactoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Factory ID is required.");
            
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(200).WithMessage("Address must not exceed 200 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^[0-9\+\-\(\)]+$").WithMessage("Invalid phone number format.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

        RuleFor(x => x.Website)
            .NotEmpty().WithMessage("Website is required.")
            .MaximumLength(100).WithMessage("Website must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.Logo)
            .NotEmpty().WithMessage("Logo URL is required.");

        RuleFor(x => x.TaxCode)
            .NotEmpty().WithMessage("Tax code is required.")
            .MaximumLength(20).WithMessage("Tax code must not exceed 20 characters.");

        RuleFor(x => x.BankAccount)
            .NotEmpty().WithMessage("Bank account is required.")
            .MaximumLength(30).WithMessage("Bank account must not exceed 30 characters.");

        RuleFor(x => x.BankName)
            .NotEmpty().WithMessage("Bank name is required.")
            .MaximumLength(100).WithMessage("Bank name must not exceed 100 characters.");
    }
}