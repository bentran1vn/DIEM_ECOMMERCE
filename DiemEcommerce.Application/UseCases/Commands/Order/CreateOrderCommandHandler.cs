using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Order;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;
using Matches = DiemEcommerce.Domain.Entities.Matches;

namespace DiemEcommerce.Application.UseCases.Commands.Order;

public class CreateOrderCommandHandler : ICommandHandler<Contract.Services.Order.Commands.CreateOrderCommand, Responses.OrderResponse>
{
    private readonly IRepositoryBase<ApplicationDbContext, Orders, Guid> _orderRepository;
    private readonly IRepositoryBase<ApplicationDbContext, OrderDetails, Guid> _orderDetailRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Matches, Guid> _matchRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Users, Guid> _userRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Factories, Guid> _factoryRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Customers, Guid> _customerRepository;
    private readonly ITransactionService _transactionService;

    public CreateOrderCommandHandler(
        IRepositoryBase<ApplicationDbContext, Orders, Guid> orderRepository,
        IRepositoryBase<ApplicationDbContext, OrderDetails, Guid> orderDetailRepository,
        IRepositoryBase<ApplicationDbContext, Matches, Guid> matchRepository,
        IRepositoryBase<ApplicationDbContext, Users, Guid> userRepository,
        IRepositoryBase<ApplicationDbContext, Factories, Guid> factoryRepository,
        IRepositoryBase<ApplicationDbContext, Customers, Guid> customerRepository,
        ITransactionService transactionService)
    {
        _orderRepository = orderRepository;
        _orderDetailRepository = orderDetailRepository;
        _matchRepository = matchRepository;
        _userRepository = userRepository;
        _factoryRepository = factoryRepository;
        _customerRepository = customerRepository;
        _transactionService = transactionService;
    }

    public async Task<Result<Responses.OrderResponse>> Handle(Contract.Services.Order.Commands.CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Validate customer exists
        var customer = await _customerRepository.FindByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null)
        {
            return Result.Failure<Responses.OrderResponse>(new Error("404", "Customer not found"));
        }

        // Get customer user to handle transactions if using wallet balance
        var customerUser = await _userRepository.FindAll(u => u.CustomerId == request.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (customerUser == null)
        {
            return Result.Failure<Responses.OrderResponse>(new Error("404", "Customer user not found"));
        }

        // Group order items by factory
        var orderItemsByFactory = new Dictionary<Guid, List<(Contract.Services.Order.Commands.OrderItemDto Item, Matches Match)>>();
        decimal totalOrderPrice = 0;

        // Validate all matches exist and calculate total price
        foreach (var item in request.OrderItems)
        {
            var match = await _matchRepository.FindByIdAsync(item.MatchId, cancellationToken);
            if (match == null || match.IsDeleted)
            {
                return Result.Failure<Responses.OrderResponse>(
                    new Error("404", $"Match with ID {item.MatchId} not found"));
            }

            if (!orderItemsByFactory.ContainsKey(match.FactoryId))
            {
                orderItemsByFactory[match.FactoryId] = new List<(Contract.Services.Order.Commands.OrderItemDto, Matches)>();
            }

            orderItemsByFactory[match.FactoryId].Add((item, match));
            totalOrderPrice += item.Price * item.Quantity;
        }

        // If using wallet payment, check if customer has enough balance
        if (request.PaymentMethod == "WalletBalance")
        {
            bool hasSufficientBalance = await _transactionService.HasSufficientBalanceAsync(
                customerUser.Id, 
                Convert.ToDouble(totalOrderPrice), 
                cancellationToken);
            
            if (!hasSufficientBalance)
            {
                return Result.Failure<Responses.OrderResponse>(
                    new Error("400", "Insufficient wallet balance"));
            }
        }

        // Create an order
        var order = new Orders
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            TotalPrice = totalOrderPrice,
            PaymentMethod = request.PaymentMethod,
            Status = 0 // Pending
        };

        _orderRepository.Add(order);

        // Create order details
        var orderDetails = new List<OrderDetails>();

        foreach (var factoryGroup in orderItemsByFactory)
        {
            var factoryId = factoryGroup.Key;
            var items = factoryGroup.Value;

            foreach (var (item, match) in items)
            {
                var orderDetail = new OrderDetails
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    MatchId = item.MatchId,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    Discount = 0, // No discount by default
                    TotalPrice = item.Price * item.Quantity
                };

                orderDetails.Add(orderDetail);
            }
        }

        _orderDetailRepository.AddRange(orderDetails);

        // If using wallet payment, create transactions
        if (request.PaymentMethod == "WalletBalance")
        {
            foreach (var factoryGroup in orderItemsByFactory)
            {
                var factoryId = factoryGroup.Key;
                var items = factoryGroup.Value;
                var factoryTotal = items.Sum(i => i.Item.Price * i.Item.Quantity);
                
                // Get factory owner user
                var factoryOwnerUser = await _userRepository.FindAll(u => u.FactoryId == factoryId)
                    .FirstOrDefaultAsync(cancellationToken);
                
                if (factoryOwnerUser == null)
                {
                    return Result.Failure<Responses.OrderResponse>(
                        new Error("404", $"Factory owner not found for factory {factoryId}"));
                }

                // Create transaction from customer to factory
                var transactionResult = await _transactionService.CreateTransactionAsync(
                    customerUser.Id,
                    factoryOwnerUser.Id,
                    Convert.ToDouble(factoryTotal),
                    $"Payment for order {order.Id}",
                    order.Id,
                    cancellationToken);
                
                if (transactionResult.IsFailure)
                {
                    return Result.Failure<Responses.OrderResponse>(transactionResult.Error);
                }
            }

            // Update order status to paid
            order.Status = 1; // Paid
        }

        // Map response
        var response = new Responses.OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = $"{customerUser.FirstName} {customerUser.LastName}",
            Address = order.Address,
            Phone = order.Phone,
            Email = order.Email,
            TotalPrice = order.TotalPrice,
            PaymentMethod = order.PaymentMethod,
            Status = order.Status,
            StatusText = GetOrderStatusText(order.Status),
            CreatedOnUtc = order.CreatedOnUtc
        };

        return Result.Success(response);
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