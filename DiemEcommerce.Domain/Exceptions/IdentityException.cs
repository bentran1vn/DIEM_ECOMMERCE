namespace DiemEcommerce.Domain.Exceptions;

public static class IdentityException
{
    public class TokenException : DomainException
    {
        public TokenException(string message) 
            : base("Token Exception", message)
        {
        }
    }
}