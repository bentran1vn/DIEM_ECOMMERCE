namespace DiemEcommerce.Contract.Services.Category;

public static class Responses
{
    public record CategoryResponse(Guid Id, string Name, string Description, Guid? ParentId, bool IsParent);
}