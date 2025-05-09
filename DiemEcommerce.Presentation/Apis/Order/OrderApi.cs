using Carter;
using DiemEcommerce.Application.DependencyInjection.Extensions;
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
            .RequireAuthorization();
        
        // POST endpoints
        group1.MapPost("", CreateOrderV1)
            .RequireAuthorization(RoleNames.Customer);
        
        group1.MapPost("sepay-payment", SePayCallBack)
            .AllowAnonymous();
        
        // GET endpoint that handles all orders based on role
        group1.MapGet("", GetOrdersV1);
        
        // GET orders by ID - accessible by all authenticated users but with role-based authorization
        group1.MapGet("{id}", GetOrderByIdV1);
        
        // GET orders with transactions - accessible by admin and factory
        group1.MapGet("{id}/transactions", GetOrderWithTransactionsV1);
        
        // PUT endpoints
        group1.MapPut("{id}/status", UpdateOrderStatusV1);
        
        // DELETE endpoints (cancel)
        group1.MapDelete("{id}", CancelOrderV1);
    }
    
    public static async Task<IResult> GetOrdersV1(ISender sender, HttpContext context, 
        int pageIndex = 1, int pageSize = 10, string? status = null)
    {
        var isAdmin = context.User.IsInRole(RoleNames.Admin);
        var isFactory = context.User.IsInRole(RoleNames.Factory);
        var isCustomer = context.User.IsInRole(RoleNames.Customer);
        
        if (isAdmin)
        {
            // Admin can see all orders - implement admin-specific query handler
            var result = await sender.Send(new Queries.GetAllOrdersQuery(
                pageIndex,
                pageSize,
                status));
                
            if (result.IsFailure)
                return HandlerFailure(result);
                
            return Results.Ok(result.Value);
        }
        else if (isFactory)
        {
            var factoryId = context.User.FindFirst("FactoryId")?.Value;
            
            if (string.IsNullOrEmpty(factoryId) || !Guid.TryParse(factoryId, out var factoryGuid))
                return Results.BadRequest("Invalid or missing factory ID");
                
            var result = await sender.Send(new Queries.GetFactoryOrdersQuery(
                factoryGuid,
                pageIndex,
                pageSize,
                status));
                
            if (result.IsFailure)
                return HandlerFailure(result);
                
            return Results.Ok(result.Value);
        }
        else if (isCustomer)
        {
            var customerId = context.User.FindFirst("CustomerId")?.Value;
            
            if (string.IsNullOrEmpty(customerId) || !Guid.TryParse(customerId, out var customerGuid))
                return Results.BadRequest("Invalid or missing customer ID");
                
            var result = await sender.Send(new Queries.GetCustomerOrdersQuery(
                customerGuid,
                pageIndex,
                pageSize,
                status));
                
            if (result.IsFailure)
                return HandlerFailure(result);
                
            return Results.Ok(result.Value);
        }
        
        return Results.Forbid();
    }
    
    public static async Task<IResult> GetOrderByIdV1(ISender sender, HttpContext context, Guid id)
    {
        var result = await sender.Send(new Queries.GetOrderByIdQuery(id));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        // Verify the user has access to the order
        var isAdmin = context.User.IsInRole(RoleNames.Admin);
        
        if (!isAdmin)
        {
            var customerId = context.User.FindFirst("CustomerId")?.Value;
            var factoryId = context.User.FindFirst("FactoryId")?.Value;
            
            if (customerId != null && Guid.TryParse(customerId, out var customerGuid))
            {
                if (result.Value.CustomerId != customerGuid)
                    return Results.Forbid();
            }
            else if (factoryId != null && Guid.TryParse(factoryId, out var factoryGuid))
            {
                // Check if any order items belong to this factory
                var hasFactoryItems = result.Value.OrderItems.Any(oi => oi.FactoryId == factoryGuid);
                if (!hasFactoryItems)
                    return Results.Forbid();
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
        // Only Admin and Factory roles can access transaction details
        var isAdmin = context.User.IsInRole(RoleNames.Admin);
        var isFactory = context.User.IsInRole(RoleNames.Factory);
        
        if (!isAdmin && !isFactory)
            return Results.Forbid();
            
        var result = await sender.Send(new Queries.GetOrderWithTransactionQuery(id));
        
        if (result.IsFailure)
            return HandlerFailure(result);

        // Factory can only see transactions for their own items
        if (!isAdmin && isFactory)
        {
            var factoryId = context.User.FindFirst("FactoryId")?.Value;
            
            if (factoryId != null && Guid.TryParse(factoryId, out var factoryGuid))
            {
                // Filter transactions to only show factory's relevant transactions
                var hasFactoryItems = result.Value.OrderItems.Any(oi => oi.FactoryId == factoryGuid);
                
                if (!hasFactoryItems)
                    return Results.Forbid();
                    
                // Optionally filter transactions to only show ones relevant to this factory
                var factoryTransactions = result.Value.Transactions
                    .Where(t => t.SenderId == factoryGuid || t.ReceiverId == factoryGuid)
                    .ToList();
                    
                result.Value.Transactions = factoryTransactions;
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
    
    public static async Task<IResult> SePayCallBack(ISender sender, [FromBody] Commands.SePayBody request)
    {
        var (type, id) = QrContentParser.TakeOrderIdFromContent(request.content);
        if (type.Equals("ORDER"))
        {
            await sender.Send(new Commands.CreateSePayOrderCommand()
            {
                orderId = id,
                transactionDate = request.transactionDate,
                transferAmount = request.transferAmount
            });
        }
        var response = new { success = true };
        return Results.Json(response);
    }
}