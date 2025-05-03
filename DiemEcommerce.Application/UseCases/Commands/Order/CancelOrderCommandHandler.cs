using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Order;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Commands.Order;

public class CancelOrderCommandHandler : ICommandHandler<Contract.Services.Order.Commands.CancelOrderCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Orders, Guid> _orderRepository;
    private readonly IRepositoryBase<ApplicationDbContext, OrderDetails, Guid> _orderDetailRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Users, Guid> _userRepository;
    private readonly ITransactionService _transactionService;

    public CancelOrderCommandHandler(
        IRepositoryBase<ApplicationDbContext, Orders, Guid> orderRepository,
        IRepositoryBase<ApplicationDbContext, OrderDetails, Guid> orderDetailRepository,
        IRepositoryBase<ApplicationDbContext, Users, Guid> userRepository,
        ITransactionService transactionService)
    {
        _orderRepository = orderRepository;
        _orderDetailRepository = orderDetailRepository;
        _userRepository = userRepository;
        _transactionService = transactionService;
    }

    public async Task<Result> Handle(Contract.Services.Order.Commands.CancelOrderCommand request, CancellationToken cancellationToken)
    {
        // Validate order exists
        var order = await _orderRepository.FindByIdAsync(request.OrderId, cancellationToken);
        if (order == null || order.IsDeleted)
        {
            return Result.Failure(new Error("404", "Order not found"));
        }

        // Verify the user is the owner of the order
        if (order.CustomersId != request.CustomerId)
        {
            return Result.Failure(new Error("403", "You are not authorized to cancel this order"));
        }

        // Check if order can be cancelled (only certain statuses)
        if (!CanCancelOrder(order.Status))
        {
            return Result.Failure(new Error("400", $"Cannot cancel order with status: {GetOrderStatusText(order.Status)}"));
        }

        // Get customer user for refunds if needed
        var customerUser = await _userRepository.FindAll(u => u.CustomersId == order.CustomersId)
            .FirstOrDefaultAsync(cancellationToken);

        if (customerUser == null)
        {
            return Result.Failure(new Error("404", "Customer user not found"));
        }

        // If order was paid, process refunds
        if (order.Status == 1 && order.PaymentMethod == "WalletBalance") // Paid with wallet
        {
            // Get all transactions related to this order
            var transactions = await _transactionService.GetOrderTransactionsAsync(order.Id, cancellationToken);
            
            if (transactions.IsFailure)
            {
                return Result.Failure(transactions.Error);
            }

            // Group transactions by factory (receiver)
            var transactionsByFactory = transactions.Value
                .GroupBy(t => t.ReceiverId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Process refunds for each factory
            foreach (var kvp in transactionsByFactory)
            {
                var factoryId = kvp.Key;
                var factoryTransactions = kvp.Value;
                var factoryUser = await _userRepository.FindByIdAsync(factoryId, cancellationToken);
                
                if (factoryUser == null)
                {
                    continue; // Skip if factory user not found
                }

                // Calculate total amount to refund from this factory
                var refundAmount = factoryTransactions.Sum(t => t.Amount);

                // Create refund transaction
                var refundResult = await _transactionService.CreateTransactionAsync(
                    factoryId,
                    customerUser.Id,
                    refundAmount,
                    $"Refund for cancelled order {order.Id}",
                    order.Id,
                    cancellationToken);
                
                if (refundResult.IsFailure)
                {
                    return Result.Failure(refundResult.Error);
                }
            }
        }

        // Update order status to cancelled
        order.Status = 5; // Cancelled
        
        // Add cancellation reason if provided
        if (!string.IsNullOrEmpty(request.CancelReason))
        {
            order.Note = (order.Note + " | Cancellation reason: " + request.CancelReason).Trim();
        }

        return Result.Success();
    }
    
    private bool CanCancelOrder(int status)
    {
        return status switch
        {
            0 => true, // Pending
            1 => true, // Paid
            2 => true, // Processing
            _ => false  // Cannot cancel Shipped, Delivered, or already Cancelled orders
        };
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