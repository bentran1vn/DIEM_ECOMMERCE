using DiemEcommerce.Contract.Abstractions.Shared;
using DiemEcommerce.Domain.Entities;

namespace DiemEcommerce.Application.Abstractions;

public interface ITransactionService
{
    /// <summary>
    /// Creates a transaction between two users
    /// </summary>
    /// <param name="senderId">ID of the sender (customer)</param>
    /// <param name="receiverId">ID of the receiver (factory or system)</param>
    /// <param name="amount">Amount to transfer</param>
    /// <param name="description">Transaction description</param>
    /// <param name="orderId">Optional order ID this transaction is for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with transaction ID if successful</returns>
    Task<Result<Guid>> CreateTransactionAsync(
        Guid senderId,
        Guid receiverId,
        double amount,
        string description,
        Guid? orderId = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a user has sufficient balance for a transaction
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <param name="amount">Amount needed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has sufficient balance</returns>
    Task<bool> HasSufficientBalanceAsync(
        Guid userId,
        double amount,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all transactions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions</returns>
    Task<Result<List<Transactions>>> GetUserTransactionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all transactions for an order
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions</returns>
    Task<Result<List<Transactions>>> GetOrderTransactionsAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}