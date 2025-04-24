using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Categories : Entity<Guid>, IAuditableEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsParent => ParentId == null;
    public virtual Categories? Parent { get; set; } = default!;
    public virtual ICollection<Categories> Children { get; set; } = new List<Categories>();
    public virtual ICollection<Matches> Matches { get; set; } = new List<Matches>();
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}