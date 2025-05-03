using DiemEcommerce.Application.Abstractions;
using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Domain.Abstractions.Repositories;
using DiemEcommerce.Domain.Entities;
using DiemEcommerce.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiemEcommerce.Infrastructure.Transactions;

public class TransactionService : ITransactionService
{
    private readonly IRepositoryBase<ApplicationDbContext, Users, Guid> _userRepository;
    private readonly IRepositoryBase<ApplicationDbContext, Domain.Entities.Transactions, Guid> _transactionRepository;

    public TransactionService(
        IRepositoryBase<ApplicationDbContext, Users, Guid> userRepository,
        IRepositoryBase<ApplicationDbContext, Domain.Entities.Transactions, Guid> transactionRepository)
    {
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<Guid>> CreateTransactionAsync(
        Guid senderId, 
        Guid receiverId, 
        double amount, 
        string description, 
        Guid? orderId = null, 
        CancellationToken cancellationToken = default)
    {
        var sender = await _userRepository.FindByIdAsync(senderId, cancellationToken);
        if (sender == null)
        {
            return Result.Failure<Guid>(new Error("404", "Sender not found"));
        }

        var receiver = await _userRepository.FindByIdAsync(receiverId, cancellationToken);
        if (receiver == null)
        {
            return Result.Failure<Guid>(new Error("404", "Receiver not found"));
        }

        if (sender.Balance < amount)
        {
            return Result.Failure<Guid>(new Error("400", "Insufficient funds"));
        }

        var transaction = new Domain.Entities.Transactions
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            CurrentBalance = sender.Balance,
            Amount = amount,
            AfterBalance = sender.Balance - amount,
            Description = description,
            TransactionType = "Transfer",
            TransactionStatus = "Success",
            OrdersId = orderId
        };

        // Update sender and receiver balance
        sender.Balance -= amount;
        receiver.Balance += amount;

        _transactionRepository.Add(transaction);

        return Result.Success(transaction.Id);
    }

    public async Task<bool> HasSufficientBalanceAsync(
        Guid userId, 
        double amount, 
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        return user != null && user.Balance >= amount;
    }

    public async Task<Result<List<Domain.Entities.Transactions>>> GetUserTransactionsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Result.Failure<List<Domain.Entities.Transactions>>(new Error("404", "User not found"));
        }

        var transactions = await _transactionRepository.FindAll(
                t => t.SenderId == userId || t.ReceiverId == userId)
            .ToListAsync(cancellationToken);

        return Result.Success(transactions);
    }

    public async Task<Result<List<Domain.Entities.Transactions>>> GetOrderTransactionsAsync(
        Guid orderId, 
        CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.FindAll(
                t => t.OrdersId == orderId)
            .ToListAsync(cancellationToken);

        return Result.Success(transactions);
    }
}