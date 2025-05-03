using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;

namespace DiemEcommerce.Contract.Services.Order;

public static class Queries
{
    public record GetAllOrdersQuery(
        int PageIndex,
        int PageSize,
        string? Status = null
    ) : IQuery<PagedResult<Responses.OrderResponse>>;
    
    // Query to get all orders for a customer
    public record GetCustomerOrdersQuery(
        Guid CustomerId,
        int PageIndex,
        int PageSize,
        string? Status = null
    ) : IQuery<PagedResult<Responses.OrderResponse>>;
    
    // Query to get all orders for a factory
    public record GetFactoryOrdersQuery(
        Guid FactoryId,
        int PageIndex,
        int PageSize,
        string? Status = null
    ) : IQuery<PagedResult<Responses.OrderResponse>>;
    
    // Query to get order details by order ID
    public record GetOrderByIdQuery(
        Guid OrderId
    ) : IQuery<Responses.OrderDetailResponse>;
    
    // Query to get order details including transaction information
    public record GetOrderWithTransactionQuery(
        Guid OrderId
    ) : IQuery<Responses.OrderTransactionResponse>;
}