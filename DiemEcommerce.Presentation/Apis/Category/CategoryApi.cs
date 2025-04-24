using Carter;
using DiemEcommerce.Contract.Services.Category;
using DiemEcommerce.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DiemEcommerce.Presentation.Apis.Category;

public class CategoryApi: ApiEndpoint, ICarterModule
{
    private const string BaseUrl = "/api/v{version:apiVersion}/category";
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group1 = app.NewVersionedApi("Category")
            .MapGroup(BaseUrl).HasApiVersion(1);
        
        group1.MapGet("", GetCategoriesV1);
        group1.MapPost("", CreateCategoryV1);
        group1.MapPut("{id}", UpdateCategoryV1);
        group1.MapDelete("{id}", DeleteCategoryV1);
    }
    
    public static async Task<IResult> GetCategoriesV1(ISender sender)
    {
        var result = await sender.Send(new Queries.GetAllCategoriesQuery());
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> CreateCategoryV1(ISender sender, [FromBody]Commands.CreateCategoryCommand command)
    {
        var result = await sender.Send(command);
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> UpdateCategoryV1(ISender sender, [FromBody]Commands.UpdateCategoryCommand command, Guid id)
    {
        if(id != command.Id)
            return Results.BadRequest("Id in route and body must be the same");
        
        var result = await sender.Send(command);
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> DeleteCategoryV1(ISender sender, [FromBody]Commands.DeleteCategoryCommand command, Guid id)
    {
        if(id != command.Id)
            return Results.BadRequest("Id in route and body must be the same");
        
        var result = await sender.Send(command);
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
}