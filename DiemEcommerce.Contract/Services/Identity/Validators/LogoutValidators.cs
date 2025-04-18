using FluentValidation;

namespace DiemEcommerce.Contract.Services.Identity.Validators;

public class LogoutValidators : AbstractValidator<Command.LogoutCommand>
{
    public LogoutValidators()
    {
        RuleFor(x => x.UserAccount).NotEmpty();
    }
}