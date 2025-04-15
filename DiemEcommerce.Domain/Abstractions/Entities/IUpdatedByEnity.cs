namespace DiemEcommerce.Domain.Abstractions.Entities;

public interface IUpdatedByEntity<T>
{
    T UpdatedBy { get; set; }
}