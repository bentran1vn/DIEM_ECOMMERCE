using System.ComponentModel.DataAnnotations.Schema;
using DiemEcommerce.Domain.Abstractions.Entities;

namespace DiemEcommerce.Domain.Entities;

public class Users : Entity<Guid>, IAuditableEntity
{
    public string Email { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string FullName => $"{FirstName} {LastName}";
    public string PhoneNumber { get; set; } = default!;
    public double Balance { get; set; } = 0;
    public Guid RolesId { get; set; }
    public virtual Roles Roles { get; set; } = default!;
    
    public Guid? FactoriesId { get; set; }
    public virtual Factories? Factories { get; set; }
    public Guid? CustomersId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual Customers? Customers { get; set; }
    
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset? ModifiedOnUtc { get; set; }
}