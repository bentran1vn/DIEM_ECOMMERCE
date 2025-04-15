using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Feedbacks: Entity<Guid>, IAuditableEntity
{
    public int Rating { get; set; }
    public string Comment { get; set; } = default!;
    public Guid OrderDetailId { get; set; }
    public OrderDetails OrderDetail { get; set; } = default!;
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}