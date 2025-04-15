using System.ComponentModel.DataAnnotations.Schema;

namespace DiemEcommerce.Domain.Abstractions.Entities;

public abstract class Entity<T> : IEntity<T>
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public T Id { get; set; }

    public bool IsDeleted { get; set; }
}
