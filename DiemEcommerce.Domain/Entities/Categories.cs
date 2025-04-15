using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Categories : Entity<Guid>, IAuditableEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsParent { get; set; } = false;
    public Categories? Parent { get; set; } = default!;
    public ICollection<Categories> Children { get; set; } = new List<Categories>();
    public ICollection<Matches> Matches { get; set; } = new List<Matches>();
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}