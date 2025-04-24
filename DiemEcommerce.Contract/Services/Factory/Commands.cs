using DiemEcommerce.Contract.Abstractions.Messages;
using Microsoft.AspNetCore.Http;

namespace DiemEcommerce.Contract.Services.Factory;

public class Commands
{
    public class CreateFactoryBody
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Description { get; set; }
        public IFormFile Logo { get; set; }
        public string TaxCode { get; set; }
        public string BankAccount { get; set; }
        public string BankName { get; set; }
    };
    
    public class CreateFactoryCommand : ICommand
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Description { get; set; }
        public IFormFile Logo { get; set; }
        public string TaxCode { get; set; }
        public string BankAccount { get; set; }
        public string BankName { get; set; }
        public  Guid UserId { get; set; }
    };
    
    public class UpdateFactoryCommand : ICommand
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Description { get; set; }
        public IFormFile Logo { get; set; }
        public string TaxCode { get; set; }
        public string BankAccount { get; set; }
        public string BankName { get; set; }
    };
        
    public record DeleteFactoryCommand(Guid Id) : ICommand;
}
