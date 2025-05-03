using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Orders: Entity<Guid>, IAuditableEntity
{
    public string? Note { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal TotalPrice { get; set; }
    public Guid CustomersId { get; set; }
    public string Status { get; set; } = default!;
    public string PayMethod { get; set; } = default!;
    // Success, Pending, Failed
    public bool IsFeedback { get; set; } = false;
    public virtual Customers Customers { get; set; } = default!;
    public virtual ICollection<OrderDetails> OrderDetails { get; set; } = new List<OrderDetails>();
    public virtual ICollection<Transactions> Transactions { get; set; } = new List<Transactions>();
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}