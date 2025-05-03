using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class FeedbackMedias : Entity<Guid>, IAuditableEntity
{
    public string Url { get; set; }
    
    public Guid FeedbacksId { get; set; }
    public virtual Feedbacks Feedbacks { get; set; } = default!;
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}