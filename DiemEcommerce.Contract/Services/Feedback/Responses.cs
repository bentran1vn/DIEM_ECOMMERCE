namespace DiemEcommerce.Contract.Services.Feedback;

public static class Responses
{
    public class FeedbackResponse
    {
        public Guid Id { get; set; }
        public Guid OrderDetailId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public List<FeedbackMediaResponse> Images { get; set; } = new List<FeedbackMediaResponse>();
        public DateTimeOffset CreatedOnUtc { get; set; }
    }
    
    public class FeedbackMediaResponse
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
    }
    
    public class UserFeedbackResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}