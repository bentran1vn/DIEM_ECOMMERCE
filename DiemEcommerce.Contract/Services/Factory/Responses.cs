namespace DiemEcommerce.Contract.Services.Factory;

public static class Responses
{
    public record FactoryResponse(
        Guid Id,
        string Name, 
        string Address, 
        string PhoneNumber, 
        string Email, 
        string Website, 
        string Description, 
        string Logo, 
        string TaxCode, 
        string BankAccount, 
        string BankName,
        Guid? UserId,
        DateTimeOffset CreatedOnUtc);
}