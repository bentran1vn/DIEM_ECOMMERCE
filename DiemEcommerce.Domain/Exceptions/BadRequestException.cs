namespace DiemEcommerce.Domain.Exceptions;

public abstract class BadRequestException : DomainException
{
    protected BadRequestException(string message)
        : base("Bad Request", message)
    {
    }
}