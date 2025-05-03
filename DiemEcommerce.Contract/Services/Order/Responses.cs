namespace DiemEcommerce.Contract.Services.Order;

public static class Responses
{
    // Basic order response
    public class OrderResponse
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public decimal TotalPrice { get; set; }
        public string PaymentMethod { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; }
        public DateTimeOffset CreatedOnUtc { get; set; }
    }
    
    public class CreateOrderResponse
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public string QrUrl { get; set; }
        public string SystemBankName { get; set; }
        public string SystemBankAccount { get; set; }
        public string SystemBankDescription { get; set; }
        public DateTimeOffset CreatedOnUtc { get; set; }
    }
    
    // Detailed order response with order items
    public class OrderDetailResponse : OrderResponse
    {
        public List<OrderItemResponse> OrderItems { get; set; } = new List<OrderItemResponse>();
    }
    
    // Order item details
    public class OrderItemResponse
    {
        public Guid Id { get; set; }
        public Guid MatchId { get; set; }
        public string MatchName { get; set; }
        public string MatchImageUrl { get; set; }
        public Guid FactoryId { get; set; }
        public string FactoryName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public bool HasFeedback { get; set; }
    }
    
    // Transaction details with order
    public class OrderTransactionResponse : OrderDetailResponse
    {
        public List<TransactionResponse> Transactions { get; set; } = new List<TransactionResponse>();
    }
    
    // Transaction information
    public class TransactionResponse
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; }
        public Guid ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public double Amount { get; set; }
        public string TransactionType { get; set; }
        public string TransactionStatus { get; set; }
        public DateTimeOffset CreatedOnUtc { get; set; }
        public string Description { get; set; }
    }
}