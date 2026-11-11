using CYHAstro.Core.Common;
using CYHAstro.Core.Entities;

namespace CYHAstro.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for wallet operations.
/// All financial mutations go through stored procedures to enforce atomicity.
/// </summary>
public interface IWalletRepository
{
    Task<Wallet?> GetByUserIdAsync(long userId, CancellationToken ct = default);

    Task<Result<WalletTransaction>> DebitAsync(
        long userId,
        decimal amount,
        string referenceType,
        long referenceId,
        string description,
        string idempotencyKey,
        CancellationToken ct = default);

    Task<Result<WalletTransaction>> CreditAsync(
        long userId,
        decimal amount,
        string balanceType,
        string referenceType,
        long? referenceId,
        string description,
        string idempotencyKey,
        CancellationToken ct = default);

    Task<IReadOnlyList<WalletTransaction>> GetTransactionHistoryAsync(
        long userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<int> GetTransactionCountAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Check if an idempotency key has already been processed.
    /// Prevents duplicate wallet credits from payment webhooks.
    /// </summary>
    Task<bool> IsIdempotencyKeyProcessedAsync(string key, CancellationToken ct = default);
}
