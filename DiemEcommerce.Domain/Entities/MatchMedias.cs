using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class MatchMedias: Entity<Guid>, IAuditableEntity
{
    public string Url { get; set; }
    
    public Guid MatchesId { get; set; }
    public virtual Matches Matches { get; set; } = default!;
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}