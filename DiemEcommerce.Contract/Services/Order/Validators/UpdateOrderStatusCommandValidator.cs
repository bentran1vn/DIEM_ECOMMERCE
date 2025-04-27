using FluentValidation;

namespace DiemEcommerce.Contract.Services.Order.Validators;

public class UpdateOrderStatusCommandValidator : AbstractValidator<Commands.UpdateOrderStatusCommand>
{
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");

        RuleFor(x => x.Status)
            .InclusiveBetween(0, 5).WithMessage("Status must be between 0 and 5.");
    }
}