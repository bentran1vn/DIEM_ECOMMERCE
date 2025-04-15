namespace DiemEcommerce.Domain.Abstractions.Entities;

public interface ICreatedByEntity<T> 
{
    T CreatedBy { get; set; }
    
}