using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Customers : Entity<Guid>, IAuditableEntity
{
    public virtual Users Users { get; set; } = null!;
    public virtual ICollection<Orders> Orders { get; set; } = new List<Orders>();
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}