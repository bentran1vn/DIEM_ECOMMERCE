using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Order;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Queries.Order;

public class GetOrderWithTransactionQueryHandler : IQueryHandler<Contract.Services.Order.Queries.GetOrderWithTransactionQuery, Responses.OrderTransactionResponse>
{
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Orders, Guid> _orderRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, OrderDetails, Guid> _orderDetailRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> _matchRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> _userRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid> _factoryRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Feedbacks, Guid> _feedbackRepository;
    private readonly IRepositoryBase<ApplicationReplicateDbContext, Transactions, Guid> _transactionRepository;

    public GetOrderWithTransactionQueryHandler(
        IRepositoryBase<ApplicationReplicateDbContext, Orders, Guid> orderRepository,
        IRepositoryBase<ApplicationReplicateDbContext, OrderDetails, Guid> orderDetailRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Matches, Guid> matchRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Users, Guid> userRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Factories, Guid> factoryRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Feedbacks, Guid> feedbackRepository,
        IRepositoryBase<ApplicationReplicateDbContext, Transactions, Guid> transactionRepository)
    {
        _orderRepository = orderRepository;
        _orderDetailRepository = orderDetailRepository;
        _matchRepository = matchRepository;
        _userRepository = userRepository;
        _factoryRepository = factoryRepository;
        _feedbackRepository = feedbackRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<Responses.OrderTransactionResponse>> Handle(Contract.Services.Order.Queries.GetOrderWithTransactionQuery request, CancellationToken cancellationToken)
    {
        // Get order with customer
        var order = await _orderRepository.FindAll(
                o => o.Id == request.OrderId && !o.IsDeleted,
                o => o.Customers)
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
        {
            return Result.Failure<Responses.OrderTransactionResponse>(new Error("404", "Order not found"));
        }

        // Get customer user
        var customerUser = await _userRepository.FindAll(u => u.CustomersId == order.CustomersId)
            .FirstOrDefaultAsync(cancellationToken);

        if (customerUser == null)
        {
            return Result.Failure<Responses.OrderTransactionResponse>(new Error("404", "Customer user not found"));
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

        // Get all transactions related to this order
        var transactions = await _transactionRepository.FindAll(
                t => t.OrdersId == order.Id)
            .ToListAsync(cancellationToken);

        // Get all user IDs involved in transactions
        var transactionUserIds = transactions
            .SelectMany(t => new[] { t.SenderId, t.ReceiverId })
            .Distinct()
            .ToList();

        // Get all users involved in transactions
        var transactionUsers = await _userRepository.FindAll(
                u => transactionUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u, cancellationToken);

        // Prepare response
        var response = new Responses.OrderTransactionResponse
        {
            Id = order.Id,
            CustomerId = order.CustomersId,
            CustomerName = $"{customerUser.FirstName} {customerUser.LastName}",
            Address = order.Address,
            Phone = order.Phone,
            Email = order.Email,
            TotalPrice = order.TotalPrice,
            PaymentMethod = order.PaymentMethod,
            Status = order.Status,
            StatusText = GetOrderStatusText(order.Status),
            CreatedOnUtc = order.CreatedOnUtc,
            OrderItems = new List<Responses.OrderItemResponse>(),
            Transactions = new List<Responses.TransactionResponse>()
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

        // Map transactions to response
        foreach (var transaction in transactions)
        {
            // Get sender and receiver names
            string senderName = "Unknown";
            string receiverName = "Unknown";

            if (transactionUsers.TryGetValue(transaction.SenderId, out var sender))
            {
                senderName = $"{sender.FirstName} {sender.LastName}";
            }

            if (transactionUsers.TryGetValue(transaction.ReceiverId, out var receiver))
            {
                receiverName = $"{receiver.FirstName} {receiver.LastName}";
            }

            var transactionResponse = new Responses.TransactionResponse
            {
                Id = transaction.Id,
                SenderId = transaction.SenderId,
                SenderName = senderName,
                ReceiverId = transaction.ReceiverId,
                ReceiverName = receiverName,
                Amount = transaction.Amount,
                TransactionType = transaction.TransactionType,
                TransactionStatus = transaction.TransactionStatus,
                CreatedOnUtc = transaction.CreatedOnUtc,
                Description = transaction.Description ?? ""
            };

            response.Transactions.Add(transactionResponse);
        }

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