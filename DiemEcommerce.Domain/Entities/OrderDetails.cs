using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class OrderDetails: Entity<Guid>, IAuditableEntity
{
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
    
    public Guid OrderId { get; set; }
    public Orders Order { get; set; } = default!;
    
    public Guid MatchId { get; set; }
    public Matches Match { get; set; } = default!;
    
    public Guid? FeedbackId { get; set; }
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}