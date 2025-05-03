using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Order;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Queries.Order;

public class GetOrderByIdQueryHandler : IQueryHandler<Contract.Services.Order.Queries.GetOrderByIdQuery, Responses.OrderDetailResponse>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Orders, Guid> _orderRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, OrderDetails, Guid> _orderDetailRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> _matchRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> _userRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid> _factoryRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Feedbacks, Guid> _feedbackRepository;

    public GetOrderByIdQueryHandler(
        IRepositoryBase<ApplicationReplicateDbContext, Orders, Guid> orderRepository,
        IRepositoryBase<ApplicationReplicateDbContext, OrderDetails, Guid> orderDetailRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> matchRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> userRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid> factoryRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Feedbacks, Guid> feedbackRepository)
    {
        _orderRepository = orderRepository;
        _orderDetailRepository = orderDetailRepository;
        _matchRepository = matchRepository;
        _userRepository = userRepository;
        _factoryRepository = factoryRepository;
        _feedbackRepository = feedbackRepository;
    }

    public async Task<Result<Responses.OrderDetailResponse>> Handle(Contract.Services.Order.Queries.GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        // Get order with customer
        var order = await _orderRepository.FindAll(
                o => o.Id == request.OrderId && !o.IsDeleted,
                o => o.Customers)
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
        {
            return Result.Failure<Responses.OrderDetailResponse>(new Error("404", "Order not found"));
        }

        // Get customer user
        var customerUser = await _userRepository.FindAll(u => u.CustomersId == order.CustomersId)
            .FirstOrDefaultAsync(cancellationToken);

        if (customerUser == null)
        {
            return Result.Failure<Responses.OrderDetailResponse>(new Error("404", "Customer user not found"));
        }

        // Get order details with matches
        var orderDetails = await _orderDetailRepository.FindAll(
                od => od.OrdersId == order.Id,
                od => od.Matches)
            .ToListAsync(cancellationToken);

        // Check if each order detail has feedback
        var orderDetailIds = orderDetails.Select(od => od.Id).ToList();
        var feedbacks = await _feedbackRepository.FindAll(f => orderDetailIds.Contains(f.OrderDetailsId))
            .ToListAsync(cancellationToken);

        var feedbackOrderDetailIds = feedbacks.Select(f => f.OrderDetailsId).ToHashSet();

        // Prepare response
        var response = new Responses.OrderDetailResponse
        {
            Id = order.Id,
            CustomerId = order.CustomersId,
            CustomerName = $"{customerUser.FirstName} {customerUser.LastName}",
            Address = order.Address,
            Phone = order.Phone,
            Email = order.Email,
            TotalPrice = order.TotalPrice,
            PaymentMethod = order.PayMethod,
            Status = order.Status,
            CreatedOnUtc = order.CreatedOnUtc,
            OrderItems = new List<Responses.OrderItemResponse>()
        };

        // Get all factory IDs from the order details
        var factoryIds = orderDetails
            .Select(od => od.Matches.FactoriesId)
            .Distinct()
            .ToList();

        // Get all factories in one query
        var factories = await _factoryRepository.FindAll(
                f => factoryIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f, cancellationToken);

        // Map order details to response
        foreach (var detail in orderDetails)
        {
            var match = detail.Matches;
            var factory = factories.GetValueOrDefault(match.FactoriesId);
            
            if (factory == null)
            {
                continue; // Skip if factory not found
            }

            // Get first image for the match
            var matchImage = match.CoverImages?.FirstOrDefault()?.Url ?? "";

            var orderItemResponse = new Responses.OrderItemResponse
            {
                Id = detail.Id,
                MatchId = detail.MatchesId,
                MatchName = match.Name,
                MatchImageUrl = matchImage,
                FactoryId = factory.Id,
                FactoryName = factory.Name,
                Quantity = detail.Quantity,
                Price = detail.Price,
                TotalPrice = detail.TotalPrice,
                HasFeedback = feedbackOrderDetailIds.Contains(detail.Id)
            };

            response.OrderItems.Add(orderItemResponse);
        }

        return Result.Success(response);
    }
}