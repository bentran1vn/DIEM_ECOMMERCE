using System.Security.Claims;
using Carter;
using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DiemEcommerce.Presentation.Apis.Identity;

using CommandV1 = Contract.Services.Identity;

public class AuthApi : ApiEndpoint, ICarterModule
{
    private const string BaseUrl = "/api/v{version:apiVersion}/auth";
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group1 = app.NewVersionedApi("Authentication")
            .MapGroup(BaseUrl).HasApiVersion(1);
        
        // group1.MapGet(string.Empty, GetCategoriesV1);
        group1.MapGet("me", GetMeV1).RequireAuthorization();;
        group1.MapPost("login", LoginV1);
        group1.MapPost("refresh_token", RefreshTokenV1);
        group1.MapGet("", GetUserV1);
        group1.MapPost("register", RegisterV1);
        group1.MapPost("forgot_password", ForgotPasswordV1);
        group1.MapPost("verify_code", VerifyCodeV1);
        group1.MapPost("change_password", ChangePasswordV1).RequireAuthorization();
        group1.MapPost("logout", LogoutV1).RequireAuthorization();
    }
    
    public static async Task<IResult> GetMeV1(ISender sender, HttpContext context, IJwtTokenService jwtTokenService)
    {
        var userId = context.User.Claims.FirstOrDefault(c => c.Type == "UserId")!.Value;
        Guid.TryParse(userId, out var guidUserId);
        
        var result = await sender.Send(new CommandV1.Query.GetMe(guidUserId));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> GetUserV1(ISender sender, int pageIndex = 1, int pageSize = 10,
        string? searchTerm = null)
    {
        var result = await sender.Send(new CommandV1.Query.GetUsers(searchTerm, pageIndex, pageSize));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> LoginV1(ISender sender, [FromBody] CommandV1.Query.Login login)
    {
        var result = await sender.Send(login);
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> RefreshTokenV1(HttpContext context, ISender sender, [FromBody] CommandV1.Query.Token query)
    {
        //var accessToken = await context.GetTokenAsync("access_token");
        var result = await sender.Send(query);
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }

    public static async Task<IResult> RegisterV1(ISender sender, [FromBody] CommandV1.Command.RegisterCommand command)
    {
        var result = await sender.Send(command);
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> LogoutV1(ISender sender, [FromBody] CommandV1.Command.LogoutCommand command)
    {
        var result = await sender.Send(command);
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> ForgotPasswordV1(ISender sender, [FromBody] CommandV1.Command.ForgotPasswordCommand command)
    {
        var result = await sender.Send(command);
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> VerifyCodeV1(ISender sender, [FromBody] CommandV1.Command.VerifyCodeCommand command)
    {
        var result = await sender.Send(command);
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
    
    public static async Task<IResult> ChangePasswordV1(ISender sender, HttpContext context, IJwtTokenService jwtTokenService, [FromBody] CommandV1.Command.ChangePasswordCommand command)
    {
        var accessToken = await context.GetTokenAsync("access_token");
        var (claimPrincipal, _)  = jwtTokenService.GetPrincipalFromExpiredToken(accessToken!);
        var email = claimPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)!.Value;
        var result = await sender.Send(new CommandV1.Command.ChangePasswordCommand(email, command.NewPassword));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        return Results.Ok(result);
    }
}