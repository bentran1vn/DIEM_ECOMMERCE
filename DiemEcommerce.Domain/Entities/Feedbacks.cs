using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Feedbacks : Entity<Guid>, IAuditableEntity
{
    public int Rating { get; set; }
    public string Comment { get; set; } = default!;
    
    public Guid OrderDetailsId { get; set; }
    public virtual OrderDetails OrderDetails { get; set; } = default!;
    
    public Guid CustomersId { get; set; }
    public virtual Customers Customers { get; set; } = default!;
    
    public virtual ICollection<FeedbackMedias> Images { get; set; } = new List<FeedbackMedias>();
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}