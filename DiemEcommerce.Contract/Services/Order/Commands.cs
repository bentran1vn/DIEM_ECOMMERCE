using DiemEcommerce.Contract.Abstractions.Messages;

namespace DiemEcommerce.Contract.Services.Order;

public static class Commands
{
    // Command to create a new order
    public class CreateOrderBody
    {
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string PaymentMethod { get; set; }
        public List<OrderItemDto> OrderItems { get; set; }
    };
    
    public class CreateOrderCommand: CreateOrderBody, ICommand<Responses.OrderResponse>
    {
        public CreateOrderCommand(Guid customerId, CreateOrderBody body)
        {
            CustomerId = customerId;
            Address = body.Address;
            Phone = body.Phone;
            Email = body.Email;
            PaymentMethod = body.PaymentMethod;
            OrderItems = body.OrderItems;
        }

        public Guid CustomerId { get; set; }
    }
    
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