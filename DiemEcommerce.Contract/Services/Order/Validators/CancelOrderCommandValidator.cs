using FluentValidation;

namespace DiemEcommerce.Contract.Services.Order.Validators;

public class CancelOrderCommandValidator : AbstractValidator<Commands.CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");
            
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");
    }
}