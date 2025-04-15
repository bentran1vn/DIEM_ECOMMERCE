using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Factories : Entity<Guid>, IAuditableEntity
{
    public string Name { get; set; }
    public string Address { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string Website { get; set; }
    public string Description { get; set; }
    public string Logo { get; set; }
    public string TaxCode { get; set; }
    public string BankAccount { get; set; }
    public string BankName { get; set; }
    
    public Guid UserId { get; set; }
    public ICollection<Matches> Matches { get; set; } = new List<Matches>();
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}