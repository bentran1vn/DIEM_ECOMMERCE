using DiemEcommerce.Contract.Abstractions.Messages;
using Microsoft.AspNetCore.Http;

namespace DiemEcommerce.Contract.Services.Match;

public class Commands
{
    public class CreateMatchBody
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public IFormFileCollection CoverImages { get; set; }
        public Guid CategoryId { get; set; } 
    }
    
    public class CreateMatchCommand : CreateMatchBody, ICommand
    {
        public CreateMatchCommand(Guid factoryId, CreateMatchBody body)
        {
            Name = body.Name;
            Description = body.Description;
            CoverImages = body.CoverImages;
            CategoryId = body.CategoryId;
            Quantity = body.Quantity;
            Price = body.Price;
            FactoryId = factoryId;
        }

        public Guid FactoryId { get; set; }
    }
    
    public class UpdateMatchBody
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Price { get; set; }
        public string Quantity { get; set; }
        public ICollection<Guid>? DeleteImages { get; set; }
        public IFormFileCollection? NewImages { get; set; }
        public Guid CategoryId { get; set; } 
    }
    
    public class UpdateMatchCommand : UpdateMatchBody, ICommand
    {
        public UpdateMatchCommand(Guid factoryId, UpdateMatchBody body)
        {
            
            Id = body.Id;
            Name = body.Name;
            Description = body.Description;
            Price = body.Price;
            Quantity = body.Quantity;
            DeleteImages = body.DeleteImages;
            NewImages = body.NewImages;
            CategoryId = body.CategoryId;
            FactoryId = factoryId;
        }

        public Guid FactoryId { get; set; }
    }
    
    public record DeleteMatchBody
    {
        public Guid Id { get; set; }
    }
    
    public record DeleteMatchCommand :DeleteMatchBody, ICommand
    {
        public DeleteMatchCommand(Guid factoryId, DeleteMatchBody body)
        {
            Id = body.Id;
            FactoryId = factoryId;
        }

        public Guid FactoryId { get; set; }
    }
}