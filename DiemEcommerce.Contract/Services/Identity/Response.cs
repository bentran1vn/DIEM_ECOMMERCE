namespace DiemEcommerce.Contract.Services.Identity;

public static class Response
{
    public class Authenticated
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTimeOffset RefreshTokenExpiryTime { get; set; }
        public GetMe User { get; set; }
    }

    public class GetMe
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string PhoneNumber { get; set; }
        public string RoleName { get; set; }
        public Guid? FactoryId { get; set; }
        public DateTimeOffset CreatedOnUtc { get; set; }
    }
}