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
    
    public class CreateFactoryCommand : CreateFactoryBody, ICommand
    {
        public CreateFactoryCommand(CreateFactoryBody body, Guid userId)
        {
            Name = body.Name;
            Address = body.Address;
            PhoneNumber = body.PhoneNumber;
            Email = body.Email;
            Website = body.Website;
            Description = body.Description;
            Logo = body.Logo;
            TaxCode = body.TaxCode;
            BankAccount = body.BankAccount;
            BankName = body.BankName;
            
            UserId = userId;
        }

        public Guid UserId { get; set; }
    };
    
    public class UpdateFactoryBody
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Description { get; set; }
        public IFormFile? Logo { get; set; }
        public string TaxCode { get; set; }
        public string BankAccount { get; set; }
        public string BankName { get; set; }
    };
    
    public class UpdateFactoryCommand : UpdateFactoryBody, ICommand
    {
        public UpdateFactoryCommand(UpdateFactoryBody body, Guid userId)
        {
            Id = body.Id;
            Name = body.Name;
            Address = body.Address;
            PhoneNumber = body.PhoneNumber;
            Email = body.Email;
            Website = body.Website;
            Description = body.Description;
            Logo = body.Logo;
            TaxCode = body.TaxCode;
            BankAccount = body.BankAccount;
            BankName = body.BankName;
            
            UserId = userId;
        }

        public  Guid UserId { get; set; }
    };
        
    public record DeleteFactoryCommand(Guid Id, Guid UserId) : ICommand;
}
