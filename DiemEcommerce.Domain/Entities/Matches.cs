using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public sealed class Matches: Entity<Guid>, IAuditableEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public ICollection<string> CoverImages { get; set; }
    
    public Guid FactoryId { get; set; }
    public Factories Factories { get; set; } = default!;
    
    public Guid CategoryId { get; set; }
    public Categories Categories { get; set; } = default!;
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}