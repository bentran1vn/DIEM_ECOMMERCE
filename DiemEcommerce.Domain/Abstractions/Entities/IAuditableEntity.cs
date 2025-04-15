namespace DiemEcommerce.Domain.Abstractions.Entities;

public interface IAuditableEntity
{
    DateTimeOffset CreatedOnUtc { get; set; }

    DateTimeOffset? ModifiedOnUtc { get; set; }
}