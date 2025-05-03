using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class OrderDetails: Entity<Guid>, IAuditableEntity
{
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
    
    public Guid OrdersId { get; set; }
    public virtual Orders Orders { get; set; } = default!;
    
    public Guid MatchesId { get; set; }
    public virtual Matches Matches { get; set; } = default!;
    
    public virtual Feedbacks Feedbacks { get; set; }
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}