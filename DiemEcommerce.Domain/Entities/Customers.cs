using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Customers : Entity<Guid>, IAuditableEntity
{
    public Guid UserId { get; set; }
    public virtual ICollection<Orders> Orders { get; set; } = new List<Orders>();
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}