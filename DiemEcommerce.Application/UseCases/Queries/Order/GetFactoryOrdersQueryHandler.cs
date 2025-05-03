using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Order;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Queries.Order;

public class GetFactoryOrdersQueryHandler : IQueryHandler<Contract.Services.Order.Queries.GetFactoryOrdersQuery, PagedResult<Responses.OrderResponse>>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Orders, Guid> _orderRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, OrderDetails, Guid> _orderDetailRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> _matchRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> _userRepository;

    public GetFactoryOrdersQueryHandler(
        IRepositoryBase<ApplicationReplicateDbContext, Orders, Guid> orderRepository,
        IRepositoryBase<ApplicationReplicateDbContext, OrderDetails, Guid> orderDetailRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> matchRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> userRepository)
    {
        _orderRepository = orderRepository;
        _orderDetailRepository = orderDetailRepository;
        _matchRepository = matchRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResult<Responses.OrderResponse>>> Handle(Contract.Services.Order.Queries.GetFactoryOrdersQuery request, CancellationToken cancellationToken)
    {
        // First get all matches from this factory
        var matchIds = await _matchRepository
            .FindAll(m => m.FactoriesId == request.FactoryId && !m.IsDeleted)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        if (matchIds.Count == 0)
        {
            // Return empty result if factory has no matches
            return Result.Success(PagedResult<Responses.OrderResponse>.Create(
                new List<Responses.OrderResponse>(), 
                request.PageIndex, 
                request.PageSize, 
                0));
        }

        // Get all order details containing matches from this factory
        var orderDetails = await _orderDetailRepository
            .FindAll(od => matchIds.Contains(od.MatchesId))
            .ToListAsync(cancellationToken);

        // Get all order IDs for this factory
        var orderIds = orderDetails
            .Select(od => od.OrdersId)
            .Distinct()
            .ToList();

        if (orderIds.Count == 0)
        {
            // Return empty result if no orders found
            return Result.Success(PagedResult<Responses.OrderResponse>.Create(
                new List<Responses.OrderResponse>(), 
                request.PageIndex, 
                request.PageSize, 
                0));
        }

        // Build query for orders
        var query = _orderRepository.FindAll(o => orderIds.Contains(o.Id) && !o.IsDeleted);

        // Apply status filter if provided
        // if (request.Status.HasValue)
        // {
        //     query = query.Where(o => o.Status == request.Status.Value);
        // }

        // Order by creation date (newest first)
        query = query.OrderByDescending(o => o.CreatedOnUtc);

        // Get customer IDs to load in a single query
        var customerIds = await query
            .Select(o => o.CustomersId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Load all customers' users in one query
        var customerUsers = await _userRepository
            .FindAll(u => customerIds.Contains(u.CustomersId.Value))
            .ToDictionaryAsync(u => u.CustomersId.Value, u => u, cancellationToken);

        // Project to response
        var ordersQuery = query.Select(o => new
        {
            Order = o,
            CustomerId = o.CustomersId
        });

        // Execute query to get paged results
        var pagedOrdersRaw = await PagedResult<dynamic>.CreateAsync(
            ordersQuery.AsQueryable(),
            request.PageIndex,
            request.PageSize);

        // Map to response objects
        var responseItems = pagedOrdersRaw.Items.Select(item =>
        {
            var order = item.Order;
            var customerUser = customerUsers.GetValueOrDefault((Guid)item.CustomerId);
            
            string customerName = "Unknown";
            if (customerUser != null)
            {
                customerName = $"{customerUser.FirstName} {customerUser.LastName}";
            }

            return new Responses.OrderResponse
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                CustomerName = customerName,
                Address = order.Address,
                Phone = order.Phone,
                Email = order.Email,
                TotalPrice = order.TotalPrice,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status,
                StatusText = GetOrderStatusText(order.Status),
                CreatedOnUtc = order.CreatedOnUtc
            };
        }).ToList();

        // Create final paged result
        var result = PagedResult<Responses.OrderResponse>.Create(
            responseItems,
            pagedOrdersRaw.PageIndex,
            pagedOrdersRaw.PageSize,
            pagedOrdersRaw.TotalCount);

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