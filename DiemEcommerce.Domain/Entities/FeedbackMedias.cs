using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class FeedbackMedias : Entity<Guid>, IAuditableEntity
{
    public string Url { get; set; }
    
    public Guid FeedbackId { get; set; }
    public virtual Feedbacks Feedback { get; set; }
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}