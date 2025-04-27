using DiemEcommerce.Contract.Abstractions.Messages;

namespace DiemEcommerce.Contract.Services.Order;

public static class Commands
{
    // Command to create a new order
    public record CreateOrderCommand(
        Guid CustomerId,
        string? Address,
        string? Phone,
        string? Email,
        string PaymentMethod,
        List<OrderItemDto> OrderItems
    ) : ICommand<Responses.OrderResponse>;
    
    // Data transfer object for order items
    public record OrderItemDto(
        Guid MatchId,
        int Quantity,
        decimal Price
    );
    
    // Command to update order status
    public record UpdateOrderStatusCommand(
        Guid OrderId,
        int Status
    ) : ICommand;
    
    // Command to cancel an order
    public record CancelOrderCommand(
        Guid OrderId,
        Guid CustomerId,
        string? CancelReason
    ) : ICommand;
}