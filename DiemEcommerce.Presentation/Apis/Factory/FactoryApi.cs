using Carter;
using DiemEcommerce.Contract.Services.Factory;
using DiemEcommerce.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DiemEcommerce.Presentation.Apis.Factory;

public class FactoryApi : ApiEndpoint, ICarterModule
{
    private const string BaseUrl = "/api/v{version:apiVersion}/factories";
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group1 = app.NewVersionedApi("Factories")
            .MapGroup(BaseUrl).HasApiVersion(1);
        
        group1.MapGet("", GetAllFactoriesV1);
        
        group1.MapGet("{id}", GetFactoryByIdV1);
        
        group1.MapPost("", CreateFactoryV1)
            .DisableAntiforgery()
            .RequireAuthorization()
            .Accepts<Commands.CreateFactoryBody>("multipart/form-data");
        
        group1.MapPut("{id}", UpdateFactoryV1)
            .DisableAntiforgery()
            .RequireAuthorization()
            .Accepts<Commands.UpdateFactoryBody>("multipart/form-data");
        
        group1.MapDelete("{id}", DeleteFactoryV1).RequireAuthorization();
    }
    
    public static async Task<IResult> GetAllFactoriesV1(ISender sender, int pageIndex = 1, int pageSize = 10,
        string? searchTerm = null)
    {
        var result = await sender.Send(new Queries.GetAllFactoriesQuery(pageIndex, pageSize, searchTerm));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> GetFactoryByIdV1(ISender sender, Guid id)
    {
        var result = await sender.Send(new Queries.GetFactoryByIdQuery(id));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> CreateFactoryV1(ISender sender,
        HttpContext context, [FromForm] Commands.CreateFactoryBody command)
    {
        var userId = context.User.FindFirst("UserId")?.Value!;
        
        var result = await sender.Send(new Commands.CreateFactoryCommand(command, new Guid(userId)));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> UpdateFactoryV1(ISender sender, HttpContext context,
        [FromForm] Commands.UpdateFactoryBody command, Guid id)
    {
        if (id != command.Id)
            return Results.BadRequest("ID in route and body must match");
        var userId = context.User.FindFirst("UserId")?.Value!;
        
        var result = await sender.Send(new Commands.UpdateFactoryCommand(command, new Guid(userId)));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> DeleteFactoryV1(ISender sender, HttpContext context, Guid id)
    {
        var userId = context.User.FindFirst("UserId")?.Value!;
        
        var result = await sender.Send(new Commands.DeleteFactoryCommand(id, new Guid(userId)));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
}