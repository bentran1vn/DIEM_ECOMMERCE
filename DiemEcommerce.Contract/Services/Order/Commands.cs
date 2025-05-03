using DiemEcommerce.Contract.Abstractions.Messages;

namespace DiemEcommerce.Contract.Services.Order;

public static class Commands
{
    // Command to create a new order
    public class CreateOrderBody
    {
        public string? Note { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool IsQR { get; set; } = false;
        public List<OrderItemDto> OrderItems { get; set; }
    };
    
    public class SePayBody
    {
        public int id { get; set; }
        public string gateway { get; set; }
        public string transactionDate { get; set; }
        public string accountNumber { get; set; }
        public string code { get; set; }
        public string content { get; set; }
        public string transferType { get; set; }
        public int transferAmount { get; set; }
        public int accumulated { get; set; }
        public string subAccount { get; set; }
        public string referenceCode { get; set; }
        public string description { get; set; }
    }
    
    public record CreateSePayOrderCommand : ICommand
    {
        public Guid orderId { get; set; }
        public string transactionDate { get; set; }
        public int transferAmount { get; set; }
    }
    
    public class CreateOrderCommand: CreateOrderBody, ICommand<Responses.CreateOrderResponse>
    {
        public CreateOrderCommand(Guid customerId, CreateOrderBody body)
        {
            CustomerId = customerId;
            Address = body.Address;
            Phone = body.Phone;
            Email = body.Email;
            Note = body.Note;
            IsQR = body.IsQR;
            OrderItems = body.OrderItems;
        }

        public Guid CustomerId { get; set; }
    }
    
    // Data transfer object for order items
    public record OrderItemDto(
        Guid MatchId,
        int Quantity
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