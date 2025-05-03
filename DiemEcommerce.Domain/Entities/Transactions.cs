using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Transactions : Entity<Guid>, IAuditableEntity
{
    public Guid ReceiverId { get; set; } // May be Customer, Factory or System
    public Guid SenderId { get; set; } // May be Customer, Factory or System
    public double CurrentBalance { get; set; }
    public double Amount { get; set; }
    public double AfterBalance { get; set; }
    public string? Description { get; set; }
    public string TransactionType { get; set; } = default!; // Transfer, Withdraw, Deposit
    public string TransactionStatus { get; set; } = default!; // Success, Pending, Failed
    
    public Guid? OrdersId { get; set; }
    public virtual Orders? Orders { get; set; }
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}