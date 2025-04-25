using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class MatchMedias: Entity<Guid>, IAuditableEntity
{
    public string Url { get; set; }
    
    public Guid MatchId { get; set; }
    public virtual Matches Matches { get; set; }
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}