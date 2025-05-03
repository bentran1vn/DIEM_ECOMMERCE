using Carter;
using DiemEcommerce.Contract.Constant.SystemRoles;
using DiemEcommerce.Contract.Services.Order;
using DiemEcommerce.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace DiemEcommerce.Presentation.Apis.Order;

public class OrderApi : ApiEndpoint, ICarterModule
{
    private const string BaseUrl = "/api/v{version:apiVersion}/orders";
    
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group1 = app.NewVersionedApi("Orders")
            .MapGroup(BaseUrl).HasApiVersion(1)
            .RequireAuthorization(); // All order endpoints require authentication
        
        // GET endpoints
        group1.MapGet("", GetCustomerOrdersV1);
        group1.MapGet("factory", GetFactoryOrdersV1);
        group1.MapGet("{id}", GetOrderByIdV1);
        group1.MapGet("{id}/transactions", GetOrderWithTransactionsV1);
        
        // POST endpoints
        group1.MapPost("", CreateOrderV1)
            .RequireAuthorization(RoleNames.Customer);
        
        // PUT endpoints
        group1.MapPut("{id}/status", UpdateOrderStatusV1);
        
        // DELETE endpoints (cancel)
        group1.MapDelete("{id}", CancelOrderV1);
    }
    
    public static async Task<IResult> GetCustomerOrdersV1(ISender sender, HttpContext context, 
        int pageIndex = 1, int pageSize = 10, int? status = null)
    {
        var customerId = context.User.FindFirst("CustomerId")?.Value;
        
        if (string.IsNullOrEmpty(customerId) || !Guid.TryParse(customerId, out var customerGuid))
        {
            return Results.BadRequest("Invalid or missing customer ID");
        }

        var result = await sender.Send(new Queries.GetCustomerOrdersQuery(
            customerGuid,
            pageIndex,
            pageSize,
            status));
        
        if (result.IsFailure)
        {
            return HandlerFailure(result);
        }

        return Results.Ok(result.Value);
    }
    
    public static async Task<IResult> GetFactoryOrdersV1(ISender sender, HttpContext context, 
        int pageIndex = 1, int pageSize = 10, int? status = null)
    {
        var factoryId = context.User.FindFirst("FactoryId")?.Value;
        
        if (string.IsNullOrEmpty(factoryId) || !Guid.TryParse(factoryId, out var factoryGuid))
        {
            return Results.BadRequest("Invalid or missing factory ID");
        }

        var result = await sender.Send(new Queries.GetFactoryOrdersQuery(
            factoryGuid,
            pageIndex,
            pageSize,
            status));
        
        if (result.IsFailure)
        {
            return HandlerFailure(result);
        }

        return Results.Ok(result.Value);
    }
    
    public static async Task<IResult> GetOrderByIdV1(ISender sender, HttpContext context, Guid id)
    {
        var result = await sender.Send(new Queries.GetOrderByIdQuery(id));
        
        if (result.IsFailure)
        {
            return HandlerFailure(result);
        }

        // Verify the user has access to the order
        var customerId = context.User.FindFirst("CustomerId")?.Value;
        var factoryId = context.User.FindFirst("FactoryId")?.Value;
        var isAdmin = context.User.IsInRole("Admin");
        
        if (!isAdmin)
        {
            if (customerId != null && Guid.TryParse(customerId, out var customerGuid))
            {
                if (result.Value.CustomerId != customerGuid)
                {
                    return Results.Forbid();
                }
            }
            else if (factoryId != null && Guid.TryParse(factoryId, out var factoryGuid))
            {
                var hasFactoryItems = result.Value.OrderItems.Any(oi => oi.FactoryId == factoryGuid);
                if (!hasFactoryItems)
                {
                    return Results.Forbid();
                }
            }
            else
            {
                return Results.Forbid();
            }
        }

        return Results.Ok(result.Value);
    }
    
    public static async Task<IResult> GetOrderWithTransactionsV1(ISender sender, HttpContext context, Guid id)
    {
        var result = await sender.Send(new Queries.GetOrderWithTransactionQuery(id));
        
        if (result.IsFailure)
        {
            return HandlerFailure(result);
        }

        // Verify the user has access to the order
        var customerId = context.User.FindFirst("CustomerId")?.Value;
        var factoryId = context.User.FindFirst("FactoryId")?.Value;
        var isAdmin = context.User.IsInRole("Admin");
        
        if (!isAdmin)
        {
            if (customerId != null && Guid.TryParse(customerId, out var customerGuid))
            {
                if (result.Value.CustomerId != customerGuid)
                {
                    return Results.Forbid();
                }
            }
            else if (factoryId != null && Guid.TryParse(factoryId, out var factoryGuid))
            {
                var hasFactoryItems = result.Value.OrderItems.Any(oi => oi.FactoryId == factoryGuid);
                if (!hasFactoryItems)
                {
                    return Results.Forbid();
                }
            }
            else
            {
                return Results.Forbid();
            }
        }

        return Results.Ok(result.Value);
    }
    
    public static async Task<IResult> CreateOrderV1(ISender sender, HttpContext context, 
        [FromBody] Commands.CreateOrderBody body)
    {
        var customerId = context.User.FindFirst("CustomerId")?.Value;
        
        if (string.IsNullOrEmpty(customerId) || !Guid.TryParse(customerId, out var customerGuid))
        {
            return Results.BadRequest("Invalid or missing customer ID");
        }

        // Override the customer ID in the command with the one from the token
        var updatedCommand = new Commands.CreateOrderCommand(customerGuid, body);
        
        var result = await sender.Send(updatedCommand);
        
        if (result.IsFailure)
        {
            return HandlerFailure(result);
        }

        // Return 201 Created with the order
        return Results.Created($"/api/v1/orders/{result.Value.Id}", result.Value);
    }
    
    public static async Task<IResult> UpdateOrderStatusV1(ISender sender, HttpContext context, 
        Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        if (id != request.OrderId)
        {
            return Results.BadRequest("Order ID in route and body must match");
        }

        // Check role-based permissions for status updates
        var isAdmin = context.User.IsInRole("Admin");
        var isSeller = context.User.IsInRole("Seller");
        var isCustomer = context.User.FindFirst("CustomerId") != null;
        
        // Determine if user has permission for this status update
        bool hasPermission = (request.Status, isAdmin, isSeller, isCustomer) switch
        {
            // Admin can change to any status
            (_, true, _, _) => true,
            
            // Seller can change to Processing, Shipped, or Delivered
            (2, _, true, _) => true, // Processing
            (3, _, true, _) => true, // Shipped
            (4, _, true, _) => true, // Delivered
            
            // Customer can only cancel
            (5, _, _, true) => true, // Cancelled
            
            // All other combinations are forbidden
            _ => false
        };

        if (!hasPermission)
        {
            return Results.Forbid();
        }

        var command = new Commands.UpdateOrderStatusCommand(request.OrderId, request.Status);
        var result = await sender.Send(command);
        
        if (result.IsFailure)
        {
            return HandlerFailure(result);
        }

        return Results.Ok();
    }
    
    public static async Task<IResult> CancelOrderV1(ISender sender, HttpContext context, Guid id)
    {
        var customerId = context.User.FindFirst("CustomerId")?.Value;
        
        if (string.IsNullOrEmpty(customerId) || !Guid.TryParse(customerId, out var customerGuid))
        {
            return Results.BadRequest("Invalid or missing customer ID");
        }

        var command = new Commands.CancelOrderCommand(id, customerGuid, null);
        var result = await sender.Send(command);
        
        if (result.IsFailure)
        {
            return HandlerFailure(result);
        }

        return Results.Ok();
    }
    
    // DTO for status update
    public class UpdateOrderStatusRequest
    {
        public Guid OrderId { get; set; }
        public int Status { get; set; }
    }
}