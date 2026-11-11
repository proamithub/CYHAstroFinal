using CYHAstro.Core.Common;
using CYHAstro.Core.Entities;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace CYHAstro.Infrastructure.Repositories;

/// <summary>
/// Dapper-based wallet repository.
/// All balance mutations use stored procedures for atomicity and audit trail.
/// </summary>
public sealed class WalletRepository : IWalletRepository
{
    private readonly IDbConnectionFactory _db;

    public WalletRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Wallet?> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Wallet>(
            "usp_GetWalletByUserId",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Result<WalletTransaction>> DebitAsync(
        long userId,
        decimal amount,
        string referenceType,
        long referenceId,
        string description,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        try
        {
            var tx = await conn.QuerySingleOrDefaultAsync<WalletTransaction>(
                "usp_DebitWallet",
                new
                {
                    UserId = userId,
                    Amount = amount,
                    ReferenceType = referenceType,
                    ReferenceId = referenceId,
                    Description = description,
                    IdempotencyKey = idempotencyKey
                },
                commandType: CommandType.StoredProcedure);

            return tx is not null
                ? Result<WalletTransaction>.Success(tx)
                : Result<WalletTransaction>.Failure("Insufficient balance or debit failed.", Result.ErrorCodes.InsufficientBalance);
        }
        catch (Exception ex)
        {
            return Result<WalletTransaction>.Failure(ex.Message);
        }
    }

    public async Task<Result<WalletTransaction>> CreditAsync(
        long userId,
        decimal amount,
        string balanceType,
        string referenceType,
        long? referenceId,
        string description,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        try
        {
            var tx = await conn.QuerySingleOrDefaultAsync<WalletTransaction>(
                "usp_CreditWallet",
                new
                {
                    UserId = userId,
                    Amount = amount,
                    BalanceType = balanceType,
                    ReferenceType = referenceType,
                    ReferenceId = referenceId,
                    Description = description,
                    IdempotencyKey = idempotencyKey
                },
                commandType: CommandType.StoredProcedure);

            return tx is not null
                ? Result<WalletTransaction>.Success(tx)
                : Result<WalletTransaction>.Failure("Credit failed.");
        }
        catch (Exception ex)
        {
            return Result<WalletTransaction>.Failure(ex.Message);
        }
    }

    public async Task<IReadOnlyList<WalletTransaction>> GetTransactionHistoryAsync(
        long userId, int page, int pageSize, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<WalletTransaction>(
            "usp_GetWalletTransactions",
            new { UserId = userId, Page = page, PageSize = pageSize },
            commandType: CommandType.StoredProcedure);
        return rows.ToList().AsReadOnly();
    }

    public async Task<int> GetTransactionCountAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM WalletTransactions WHERE UserId = @UserId AND IsDeleted = 0",
            new { UserId = userId });
    }

    public async Task<bool> IsIdempotencyKeyProcessedAsync(string key, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<bool>(
            "SELECT CAST(COUNT(1) AS BIT) FROM WalletTransactions WHERE IdempotencyKey = @Key",
            new { Key = key });
    }
}
