using Carter;
using DiemEcommerce.Contract.Services.Feedback;
using DiemEcommerce.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DiemEcommerce.Presentation.Apis.Feedback;

public class FeedbackApi : ApiEndpoint, ICarterModule
{
    private const string BaseUrl = "/api/v{version:apiVersion}/feedback";
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group1 = app.NewVersionedApi("Feedback")
            .MapGroup(BaseUrl).HasApiVersion(1);
        
        // GET endpoints - public access for viewing feedback
        group1.MapGet("product/{matchId}", GetFeedbackByProductV1);
        group1.MapGet("customer/{customerId}", GetFeedbackByCustomerV1).RequireAuthorization();
        group1.MapGet("{id}", GetFeedbackByIdV1);
        
        // POST, PUT, DELETE endpoints - require authentication
        var authenticatedGroup = group1.RequireAuthorization();
        
        // POST endpoint for creating feedback
        authenticatedGroup.MapPost("", CreateFeedbackV1)
            .DisableAntiforgery()
            .Accepts<FeedbackCreateRequest>("multipart/form-data");
        
        // PUT endpoint for updating feedback
        authenticatedGroup.MapPut("{id}", UpdateFeedbackV1)
            .DisableAntiforgery()
            .Accepts<FeedbackUpdateRequest>("multipart/form-data");
        
        // DELETE endpoint for deleting feedback
        authenticatedGroup.MapDelete("{id}", DeleteFeedbackV1);
    }
    
    public static async Task<IResult> CreateFeedbackV1(ISender sender, HttpContext context, 
        [FromForm] FeedbackCreateRequest request)
    {
        var customerId = context.User.FindFirst("CustomerId")?.Value;
        
        if (string.IsNullOrEmpty(customerId) || !Guid.TryParse(customerId, out var customerGuid))
        {
            return Results.BadRequest("Invalid or missing customer ID");
        }

        var command = new Commands.CreateFeedbackCommand(
            request.OrderDetailId,
            customerGuid,
            request.Rating,
            request.Comment,
            request.Images);
        
        var result = await sender.Send(command);
        
        if (result.IsFailure)
        {
            return HandlerFailure(result);
        }

        return Results.Created($"/api/v1/feedback/{result.Value.Id}", result.Value);
    }
    
    public static async Task<IResult> UpdateFeedbackV1(ISender sender, HttpContext context, 
        Guid id, [FromForm] FeedbackUpdateRequest request)
    {
        var customerId = context.User.FindFirst("CustomerId")?.Value;
        
        if (string.IsNullOrEmpty(customerId) || !Guid.TryParse(customerId, out var customerGuid))
        {
            return Results.BadRequest("Invalid or missing customer ID");
        }

        if (id != request.FeedbackId)
        {
            return Results.BadRequest("Feedback ID in route and body must match");
        }

        var command = new Commands.UpdateFeedbackCommand(
            request.FeedbackId,
            customerGuid,
            request.Rating,
            request.Comment,
            request.NewImages,
            request.DeleteImages);
        
        var result = await sender.Send(command);
        
        if (result.IsFailure)
        {
            return HandlerFailure(result);
        }

        return Results.Ok(result.Value);
    }
    
    public static async Task<IResult> DeleteFeedbackV1(ISender sender, HttpContext context, Guid id)
    {
        var customerId = context.User.FindFirst("CustomerId")?.Value;
        
        if (string.IsNullOrEmpty(customerId) || !Guid.TryParse(customerId, out var customerGuid))
        {
            return Results.BadRequest("Invalid or missing customer ID");
        }

        var command = new Commands.DeleteFeedbackCommand(id, customerGuid);
        var result = await sender.Send(command);
        
        if (result.IsFailure)
        {
            return HandlerFailure(result);
        }

        return Results.NoContent();
    }
    
    public static async Task<IResult> GetFeedbackByProductV1(ISender sender, Guid matchId, 
        int pageIndex = 1, int pageSize = 10)
    {
        var query = new Queries.GetFeedbackByProductQuery(matchId, pageIndex, pageSize);
        var result = await sender.Send(query);
        
        if (result.IsFailure)
        {
            return HandlerFailure(result);
        }

        return Results.Ok(result.Value);
    }
    
    public static async Task<IResult> GetFeedbackByCustomerV1(ISender sender, HttpContext context, 
        Guid customerId, int pageIndex = 1, int pageSize = 10)
    {
        // Check authorization - users can only view their own feedback unless they're admin
        var currentUserId = context.User.FindFirst("UserId")?.Value;
        var currentUserCustomerId = context.User.FindFirst("CustomerId")?.Value;
        var isAdmin = context.User.IsInRole("Admin");
        
        // Convert current user's customer ID to Guid if present
        if (!isAdmin && !string.IsNullOrEmpty(currentUserCustomerId) && 
            Guid.TryParse(currentUserCustomerId, out var currentCustomerGuid))
        {
            // If not admin and trying to access another customer's feedback
            if (currentCustomerGuid != customerId)
            {
                return Results.Forbid();
            }
        }
        else if (!isAdmin)
        {
            return Results.Forbid();
        }

        var query = new Queries.GetFeedbackByCustomerQuery(customerId, pageIndex, pageSize);
        var result = await sender.Send(query);
        
        if (result.IsFailure)
        {
            return HandlerFailure(result);
        }

        return Results.Ok(result.Value);
    }
    
    public static async Task<IResult> GetFeedbackByIdV1(ISender sender, Guid id)
    {
        var query = new Queries.GetFeedbackByIdQuery(id);
        var result = await sender.Send(query);
        
        if (result.IsFailure)
        {
            return HandlerFailure(result);
        }

        return Results.Ok(result.Value);
    }
    
    // Request DTOs
    public class FeedbackCreateRequest
    {
        public Guid OrderDetailId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public IFormFileCollection? Images { get; set; }
    }
    
    public class FeedbackUpdateRequest
    {
        public Guid FeedbackId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public IFormFileCollection? NewImages { get; set; }
        public List<Guid>? DeleteImages { get; set; }
    }
}