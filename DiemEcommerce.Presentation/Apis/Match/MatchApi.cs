using Carter;
using DiemEcommerce.Contract.Services.Match;
using DiemEcommerce.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DiemEcommerce.Presentation.Apis.Match;

public class MatchApi: ApiEndpoint, ICarterModule
{
    private const string BaseUrl = "/api/v{version:apiVersion}/matches";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group1 = app.NewVersionedApi("Matches")
            .MapGroup(BaseUrl).HasApiVersion(1);
        
        group1.MapGet("", GetMatchesV1);
        group1.MapGet("{id}", GetMatchesByIdV1);
        group1.MapPost("", CreateMatchV1);
        group1.MapPut("{id}", UpdateMatchV1);
        group1.MapDelete("{id}", DeleteMatchV1);
    }
    
    public static async Task<IResult> GetMatchesV1(ISender sender, int pageIndex = 1, int pageSize = 10,
        string? searchTerm = null, Guid? categoryId = null)
    {
        var result = await sender.Send(new Queries.GetAllMatchQuery(categoryId, searchTerm, pageIndex, pageSize));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> GetMatchesByIdV1(ISender sender, Guid id)
    {
        var result = await sender.Send(new Queries.GetMatchByIdQuery(id));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> CreateMatchV1(ISender sender, [FromForm]Commands.CreateMatchBody command, HttpContext context)
    {
        var factoryId = context.User.FindFirst("FactoryId")?.Value!;
        
        if (!Guid.TryParse(factoryId, out var factoryGuid))
            return Results.BadRequest("Invalid FactoryId format.");
        
        var result = await sender.Send(new Commands.CreateMatchCommand(factoryGuid, command));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> UpdateMatchV1(ISender sender, [FromForm]Commands.UpdateMatchBody command,
        HttpContext context, Guid id)
    {
        if (id != command.Id)
            return Results.BadRequest("ID in route and body must match");
        
        var factoryId = context.User.FindFirst("FactoryId")?.Value!;
        
        if (!Guid.TryParse(factoryId, out var factoryGuid))
            return Results.BadRequest("Invalid FactoryId format.");
        
        var result = await sender.Send(new Commands.UpdateMatchCommand(factoryGuid, command));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> DeleteMatchV1(ISender sender, [FromBody]Commands.DeleteMatchBody command,
        HttpContext context, Guid id)
    {
        if (id != command.Id)
            return Results.BadRequest("ID in route and body must match");
        
        var factoryId = context.User.FindFirst("FactoryId")?.Value!;
        
        if (!Guid.TryParse(factoryId, out var factoryGuid))
            return Results.BadRequest("Invalid FactoryId format.");
        
        var result = await sender.Send(new Commands.DeleteMatchCommand(factoryGuid, command));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
}