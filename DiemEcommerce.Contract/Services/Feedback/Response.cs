namespace DiemEcommerce.Contract.Services.Feedback;

public class Response
{
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