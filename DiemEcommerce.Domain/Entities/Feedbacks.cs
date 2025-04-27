using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Feedbacks : Entity<Guid>, IAuditableEntity
{
    public int Rating { get; set; }
    public string Comment { get; set; } = default!;
    
    public Guid OrderDetailId { get; set; }
    public virtual OrderDetails OrderDetail { get; set; } = default!;
    
    public Guid CustomerId { get; set; }
    public virtual Customers Customer { get; set; }
    
    public virtual ICollection<FeedbackMedias> Images { get; set; } = new List<FeedbackMedias>();
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}