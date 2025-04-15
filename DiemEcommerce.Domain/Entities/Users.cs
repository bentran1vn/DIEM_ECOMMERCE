using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public sealed class Users : Entity<Guid>, IAuditableEntity
{
    public string Email { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string FullName => $"{FirstName} {LastName}";
    public string PhoneNumber { get; set; } = default!;
    
    public Guid RoleId { get; set; }
    public Roles Roles { get; set; } = default!;
    
    public Guid? FactoryId { get; set; }
    public Factories? Factories { get; set; }
   
    public Guid? CustomerId { get; set; }
    public Customers? Customers { get; set; }
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}