using DiemEcommerce.Contract.Abstractions.Messages;

namespace DiemEcommerce.Contract.Services.Category;

public static class Queries
{
    public record GetAllCategoriesQuery : IQuery<List<Responses.CategoryResponse>>;
}