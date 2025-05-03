using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Order;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Queries.Order;

public class GetAllOrdersQueryHandler : IQueryHandler<Contract.Services.Order.Queries.GetAllOrdersQuery, PagedResult<Responses.OrderResponse>>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Orders, Guid> _orderRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> _userRepository;

    public GetAllOrdersQueryHandler(
        IRepositoryBase<ApplicationReplicateDbContext, Orders, Guid> orderRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> userRepository)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResult<Responses.OrderResponse>>> Handle(Contract.Services.Order.Queries.GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        // Build query for all orders
        var query = _orderRepository.FindAll(o => !o.IsDeleted);

        // Apply status filter if provided
        if (request.Status != null)
        {
            query = query.Where(o => o.Status == request.Status);
        }

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
                CustomerId = order.CustomersId,
                CustomerName = customerName,
                Address = order.Address ?? "",
                Phone = order.Phone ?? "",
                Email = order.Email ?? "",
                TotalPrice = order.TotalPrice,
                PaymentMethod = order.PayMethod, // This should be adapted based on your actual data model
                Status = order.Status,
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
}