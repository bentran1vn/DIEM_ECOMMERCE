using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Orders: Entity<Guid>, IAuditableEntity
{
    public string? Note { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal TotalPrice { get; set; }
    public string PaymentMethod { get; set; } = default!;
    public int Status { get; set; }
    
    public Guid CustomerId { get; set; }
    public virtual Customers Customer { get; set; } = default!;
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}