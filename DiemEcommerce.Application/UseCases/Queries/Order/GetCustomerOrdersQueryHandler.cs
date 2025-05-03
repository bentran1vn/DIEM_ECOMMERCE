using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Order;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Queries.Order;

public class GetCustomerOrdersQueryHandler : IQueryHandler<Contract.Services.Order.Queries.GetCustomerOrdersQuery, PagedResult<Responses.OrderResponse>>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Orders, Guid> _orderRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> _userRepository;

    public GetCustomerOrdersQueryHandler(
        IRepositoryBase<ApplicationReplicateDbContext, Orders, Guid> orderRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> userRepository)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResult<Responses.OrderResponse>>> Handle(Contract.Services.Order.Queries.GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        // Get customer user
        var customerUser = await _userRepository.FindAll(u => u.CustomersId == request.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (customerUser == null)
        {
            return Result.Failure<PagedResult<Responses.OrderResponse>>(new Error("404", "Customer user not found"));
        }

        // Build query for orders
        var query = _orderRepository.FindAll(o => o.CustomersId == request.CustomerId && !o.IsDeleted);

        // Apply status filter if provided
        // if (request.Status.HasValue)
        // {
        //     query = query.Where(o => o.Status == request.Status.Value);
        // }

        // Order by creation date (newest first)
        query = query.OrderByDescending(o => o.CreatedOnUtc);

        // Project to response
        var ordersQuery = query.Select(o => new Responses.OrderResponse
        {
            Id = o.Id,
            CustomerId = o.CustomersId,
            CustomerName = $"{customerUser.FirstName} {customerUser.LastName}",
            Address = o.Address,
            Phone = o.Phone,
            Email = o.Email,
            TotalPrice = o.TotalPrice,
            PaymentMethod = "o.PaymentMethod",
            StatusText = "",
            CreatedOnUtc = o.CreatedOnUtc
        });

        // Create paged result
        var result = await PagedResult<Responses.OrderResponse>.CreateAsync(
            ordersQuery,
            request.PageIndex,
            request.PageSize);

        return Result.Success(result);
    }
    
    private string GetOrderStatusText(int status)
    {
        return status switch
        {
            0 => "Pending",
            1 => "Paid",
            2 => "Processing",
            3 => "Shipped",
            4 => "Delivered",
            5 => "Cancelled",
            _ => "Unknown"
        };
    }
}