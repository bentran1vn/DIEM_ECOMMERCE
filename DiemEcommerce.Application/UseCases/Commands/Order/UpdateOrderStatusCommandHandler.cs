using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Contract.Services.Order;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;

namespace DiemEcommerce.Application.UseCases.Commands.Order;

public class UpdateOrderStatusFinalHandler : ICommandHandler<Contract.Services.Order.Commands.UpdateOrderStatusCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Orders, Guid> _orderRepository;

    public UpdateOrderStatusFinalHandler(
        IRepositoryBase<ApplicationDbContext, Orders, Guid> orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(Contract.Services.Order.Commands.UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        // Validate order exists
        var order = await _orderRepository.FindByIdAsync(request.OrderId, cancellationToken);
        if (order == null || order.IsDeleted)
        {
            return Result.Failure(new Error("404", "Order not found"));
        }

        // Validate status is valid
        if (!IsValidOrderStatus(request.Status))
        {
            return Result.Failure(new Error("400", "Invalid order status"));
        }

        // Validate status transition is allowed
        if (!IsValidStatusTransition(order.Status, request.Status))
        {
            return Result.Failure(new Error("400", $"Cannot transition from status {GetOrderStatusText(order.Status)} to {GetOrderStatusText(request.Status)}"));
        }

        // Update order status
        order.Status = request.Status;

        return Result.Success();
    }
    
    private bool IsValidOrderStatus(int status)
    {
        return status switch
        {
            0 => true, // Pending
            1 => true, // Paid
            2 => true, // Processing
            3 => true, // Shipped
            4 => true, // Delivered
            5 => true, // Cancelled
            _ => false
        };
    }
    
    private bool IsValidStatusTransition(int currentStatus, int newStatus)
    {
        return (currentStatus, newStatus) switch
        {
            // Valid transitions
            (0, 1) => true, // Pending to Paid
            (0, 5) => true, // Pending to Cancelled
            (1, 2) => true, // Paid to Processing
            (1, 5) => true, // Paid to Cancelled
            (2, 3) => true, // Processing to Shipped
            (2, 5) => true, // Processing to Cancelled
            (3, 4) => true, // Shipped to Delivered
            (3, 5) => true, // Shipped to Cancelled
            
            // Allow same status (no change)
            var (current, next) when current == next => true,
            
            // All other transitions are invalid
            _ => false
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