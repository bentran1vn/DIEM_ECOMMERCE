namespace DiemEcommerce.Domain.Exceptions;

public static class ProductException
{
    public class ProductNotFoundException : NotFoundException
    {
        public ProductNotFoundException(Guid productId)
            : base($"The product with the id {productId} was not found.") { }
    }

    public class ProductFieldException : NotFoundException
    {
        public ProductFieldException(string productField)
            : base($"The product with the field {productField} is not correct.") { }
    }
}