using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Matches: Entity<Guid>, IAuditableEntity
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required decimal Price { get; set; }
    public required int Quantity { get; set; }
    public virtual ICollection<MatchMedias> CoverImages { get; set; } = default!;
    
    public Guid FactoriesId { get; set; }
    public virtual Factories Factories { get; set; } = default!;
    
    public Guid CategoriesId { get; set; }
    public virtual Categories Categories { get; set; } = default!;
    
    public virtual ICollection<OrderDetails> OrderDetails { get; set; } = new List<OrderDetails>();
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}