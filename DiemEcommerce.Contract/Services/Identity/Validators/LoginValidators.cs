using FluentValidation;

namespace DiemEcommerce.Contract.Services.Identity.Validators;

public class LoginValidators : AbstractValidator<Query.Login>
{
    public LoginValidators()
    {
        RuleFor(x => x.EmailOrUserName).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}