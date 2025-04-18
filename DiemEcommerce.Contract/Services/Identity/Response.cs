namespace DiemEcommerce.Contract.Services.Identity;

public static class Response
{
    public class Authenticated
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTimeOffset RefreshTokenExpiryTime { get; set; }
    }

    public class GetMe
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Phonenumber { get; set; }
        public DateTimeOffset CreatedOnUtc { get; set; }
        public Subscription Subcription { get; set; }
    }

    public class Subscription
    {
        public string SubscriptionName { get; set; }
        public DateTimeOffset SubscriptionEndDate { get; set; }
    }
}