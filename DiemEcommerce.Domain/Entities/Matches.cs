using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Matches: Entity<Guid>, IAuditableEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public virtual ICollection<MatchMedias> CoverImages { get; set; }
    
    public Guid FactoryId { get; set; }
    public virtual Factories Factories { get; set; }
    
    public Guid CategoryId { get; set; }
    public virtual Categories Categories { get; set; }
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}