using DiemEcommerce.Contract.Abstractions.Messages;

namespace DiemEcommerce.Contract.Services.Category;

public static class Commands
{
    public record CreateCategoryCommand(string Name, string Description, Guid? ParentId, bool IsParent) : ICommand;
    public record UpdateCategoryCommand(Guid Id ,string Name, string Description, Guid? ParentId) : ICommand;
    public record DeleteCategoryCommand(Guid Id ) : ICommand;
}