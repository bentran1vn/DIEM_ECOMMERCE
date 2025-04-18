using FluentValidation;

namespace DiemEcommerce.Contract.Services.Identity.Validators;

public class RegisterValidators : AbstractValidator<Command.RegisterCommand>
{
    public RegisterValidators()
    {
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().NotEmpty();
        RuleFor(x => x.Phonenumber).NotEmpty();
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.Role).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1).NotNull();
    }
}