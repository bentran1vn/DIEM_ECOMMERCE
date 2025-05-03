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

public class CreateOrderCommandHandler : ICommandHandler<Contract.Services.Order.Commands.CreateOrderCommand, Responses.CreateOrderResponse>
{
    private readonly IRepositoryBase<ApplicationDbContext, Orders, Guid> _orderRepository;
    private readonly IRepositoryBase<ApplicationDbContext, OrderDetails, Guid> _orderDetailRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Matches, Guid> _matchRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Customers, Guid> _customerRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Transactions, Guid> _transactionRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Factories, Guid> _factoriesRepository;

    public CreateOrderCommandHandler(
        IRepositoryBase<ApplicationDbContext, Orders, Guid> orderRepository,
        IRepositoryBase<ApplicationDbContext, OrderDetails, Guid> orderDetailRepository,
        IRepositoryBase<ApplicationDbContext, Matches, Guid> matchRepository,
        IRepositoryBase<ApplicationDbContext, Customers, Guid> customerRepository, IRepositoryBase<ApplicationDbContext, Transactions, Guid> transactionRepository, IRepositoryBase<ApplicationDbContext, Factories, Guid> factoriesRepository)
    {
        _orderRepository = orderRepository;
        _orderDetailRepository = orderDetailRepository;
        _matchRepository = matchRepository;
        _customerRepository = customerRepository;
        _transactionRepository = transactionRepository;
        _factoriesRepository = factoriesRepository;
    }

    public async Task<Result<Responses.CreateOrderResponse>> Handle(Contract.Services.Order.Commands.CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Validate customer exists
        var customer = await _customerRepository
            .FindByIdAsync(request.CustomerId, cancellationToken, x => x.Users);
        
        if (customer == null)
        {
            return Result.Failure<Responses.CreateOrderResponse>(new Error("404", "Customer not found"));
        }

        var matchIds = request.OrderItems.Select(x => x.MatchId);
        
        // Validate matches exist
        var matches = await _matchRepository.FindAll(m => matchIds.Contains(m.Id))
            .ToListAsync(cancellationToken);
        
        if (matches.Count != request.OrderItems.Count)
        {
            return Result.Failure<Responses.CreateOrderResponse>(new Error("404", "Some matches not found"));
        }
        
        var totalPrice = request.OrderItems.Sum(x => x.Quantity * matches.First(m => m.Id == x.MatchId).Price);

        if (!request.IsQR)
        {
            if(customer.Users.Balance < totalPrice)
            {
                return Result.Failure<Responses.CreateOrderResponse>(new Error("400", "Insufficient balance"));
            }
        }
        
        var order = new Orders
        {
            Id = Guid.NewGuid(),
            CustomersId = request.CustomerId,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            Note = request.Note,
            TotalPrice = totalPrice,
            IsFeedback = false,
            Status = request.IsQR ? "Pending" : "Success",
        };
        
        _orderRepository.Add(order);

        var orderDetail = request.OrderItems.Select(x => new OrderDetails()
        {
            Id = Guid.NewGuid(),
            OrdersId = order.Id,
            MatchesId = x.MatchId,
            Quantity = x.Quantity,
            Discount = 0,
            Price = matches.First(m => m.Id == x.MatchId).Price,
        });
        
        _orderDetailRepository.AddRange(orderDetail);


        var factoryMap = new Dictionary<Guid, decimal>();
        
        foreach (var match in matches)
        {
            if (!factoryMap.ContainsKey(match.FactoriesId))
            {
                factoryMap[match.FactoriesId] = 0;
            }
            factoryMap[match.FactoriesId] += match.Price * request.OrderItems.First(x => x.MatchId == match.Id).Quantity;
        }
        
        var updateMatchQuan = matches.Select(x =>
        {
            var orderItem = request.OrderItems.FirstOrDefault(y => y.MatchId == x.Id);
            if (orderItem != null)
            {
                x.Quantity -= orderItem.Quantity;
            }
            return x;
        });

        var currentUserBalance = customer.Users.Balance;

        List<Transactions> trans;
        
        if (request.IsQR)
        {
            trans = factoryMap.Select(x => new Transactions()
            {
                Id = Guid.NewGuid(),
                SenderId = customer.Id,
                ReceiverId = x.Key,
                CurrentBalance = currentUserBalance,
                Amount = x.Value,
                AfterBalance = currentUserBalance,
                Description = $"Order {order.Id}",
                OrdersId = order.Id,
                TransactionType = "Transfer",
                TransactionStatus = "Pending",
                Method = "External",
            }).ToList();
        }
        else
        {
            trans = factoryMap.Select(x => new Transactions()
            {
                Id = Guid.NewGuid(),
                SenderId = customer.Id,
                ReceiverId = x.Key,
                CurrentBalance = currentUserBalance,
                Amount = x.Value,
                AfterBalance = currentUserBalance - x.Value,
                Description = $"Order {order.Id}",
                OrdersId = order.Id,
                TransactionType = "Transfer",
                TransactionStatus = "Success",
                Method = "Internal",
            }).ToList();
            
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
        
        customer.Users.Balance -= totalPrice;
        _transactionRepository.AddRange(trans);
        _matchRepository.UpdateRange(updateMatchQuan);
        
        var urlSea = $"https://qr.sepay.vn/img?bank=MBBank&acc=0901928382&template=&amount={totalPrice}&des=DiemOrder{order.Id}";

        var response = new Responses.CreateOrderResponse()
        {
            Id = order.Id,
            CustomerId = order.CustomersId,
            CustomerName = $"{customer.Users.FirstName} {customer.Users.LastName}",
            Address = order.Address,
            Phone = order.Phone,
            Email = order.Email,
            TotalPrice = order.TotalPrice,
            Status = order.Status,
            QrUrl = !request.IsQR ? "" : urlSea,
            SystemBankName = !request.IsQR ? "" : "MB Bank",
            SystemBankAccount = !request.IsQR ? "" : "0901928382",
            SystemBankDescription = !request.IsQR ? "" : $"DiemOrder{order.Id}",
            CreatedOnUtc = order.CreatedOnUtc
        };
        
        return Result.Success(response);
    }


}