using DiemEcommerce.Contract.Abstractions.Messages;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Application.UseCases.Commands.Order;

public class CreateSePayOrderCommandHandler: ICommandHandler<Contract.Services.Order.Commands.CreateSePayOrderCommand>
{
    private readonly IRepositoryBase<ApplicationDbContext, Orders, Guid> _orderRepository;
    private readonly IRepositoryBase<ApplicationDbContext, OrderDetails, Guid> _orderDetailRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Matches, Guid> _matchesRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Transactions, Guid> _transactionRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Factories, Guid> _factoriesRepository;

    public CreateSePayOrderCommandHandler(IRepositoryBase<ApplicationDbContext, Orders, Guid> orderRepository, IRepositoryBase<ApplicationDbContext, OrderDetails, Guid> orderDetailRepository, IRepositoryBase<ApplicationDbContext, Matches, Guid> matchesRepository, IRepositoryBase<ApplicationDbContext, Transactions, Guid> transactionRepository, IRepositoryBase<ApplicationDbContext, Factories, Guid> factoriesRepository)
    {
        _orderRepository = orderRepository;
        _orderDetailRepository = orderDetailRepository;
        _matchesRepository = matchesRepository;
        _transactionRepository = transactionRepository;
        _factoriesRepository = factoriesRepository;
    }

    public async Task<Result> Handle(Contract.Services.Order.Commands.CreateSePayOrderCommand request, CancellationToken cancellationToken)
    {
        var query =  _orderRepository.FindAll(x => x.Id.Equals(request.orderId)).AsTracking();
        
        query = query
            .Include(x => x.OrderDetails)
            .ThenInclude(x => x.Matches)
            .Include(x => x.Transactions);
        
        var order = await query.FirstOrDefaultAsync(cancellationToken);
            
        if (order == null || order.IsDeleted)
        {
            return Result.Failure(new Error("404", "Order not found"));
        }
        
        var isPaymentSuccessful = Math.Round(order.TotalPrice, 2).Equals(Math.Round(Convert.ToDecimal(request.transferAmount), 2));

        if (isPaymentSuccessful)
        {
            order.Status = "Success";
            
            var factoryMap = new Dictionary<Guid, decimal>();
            
            foreach (var match in order.OrderDetails)
            {
                if (!factoryMap.ContainsKey(match.Matches.FactoriesId))
                {
                    factoryMap[match.Matches.FactoriesId] = 0;
                }
                factoryMap[match.Matches.FactoriesId] += match.TotalPrice;
            }

            order.Transactions = order.Transactions.Select(
                x =>
                {
                    if (factoryMap.TryGetValue(x.Id, out var value))
                    {
                        x.Amount = value;
                        x.TransactionStatus = "Success";
                    }

                    return x;
                }
            ).ToList();
            
            var factory = await _factoriesRepository.FindAll(x => factoryMap.Keys.Contains(x.Id))
                .Include(x => x.Users)
                .ToListAsync(cancellationToken);
            
            factory = factory.Select(x =>
            {
                x.Users.Balance += factoryMap[x.Id];
                return x;
            }).ToList();
            
            _factoriesRepository.UpdateRange(factory);
        } 
        else
        {
            order.Status = "Failed";
            var matchBuys = order.OrderDetails.Select(x =>
            {
                x.Matches.Quantity -= x.Quantity;
                return x.Matches;
            }).ToList();
            
            _matchesRepository.UpdateRange(matchBuys);
        }

        return Result.Success();
    }
}