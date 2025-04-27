using DiemEcommerce.Contract.Services.Order;
using FluentValidation;

namespace DiemEcommerce.Contract.Services.Order.Validators;

public class CreateOrderCommandValidator : AbstractValidator<Commands.CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");
            
        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required.");
            
        RuleFor(x => x.OrderItems)
            .NotEmpty().WithMessage("Order must contain at least one item.");
            
        RuleForEach(x => x.OrderItems).ChildRules(item =>
        {
            item.RuleFor(x => x.MatchId)
                .NotEmpty().WithMessage("Match ID is required for each order item.");
                
            item.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
                
            item.RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than zero.");
        });
        
        When(x => x.PaymentMethod == "WalletBalance", () =>
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("Customer ID is required for wallet payments.");
        });
    }
}