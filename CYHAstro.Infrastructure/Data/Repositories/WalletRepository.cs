using System.Data;
using CYHAstro.Core.Common;
using CYHAstro.Core.Entities;
using CYHAstro.Core.Interfaces.Repositories;
using Dapper;

namespace CYHAstro.Infrastructure.Data.Repositories;

public sealed class WalletRepository : IWalletRepository
{
    private readonly DbConnectionFactory _db;

    public WalletRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Wallet?> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Wallet>(
            "dbo.usp_GetWalletByUserId",
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
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);
            var tx = await conn.QuerySingleOrDefaultAsync<WalletTransaction>(
                "dbo.usp_DebitWallet",
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

            if (tx == null)
            {
                return Result<WalletTransaction>.Failure("Wallet debit failed.", Result.ErrorCodes.DatabaseError);
            }

            return Result<WalletTransaction>.Success(tx);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Insufficient wallet balance", StringComparison.OrdinalIgnoreCase))
            {
                return Result<WalletTransaction>.Failure("Insufficient wallet balance.", Result.ErrorCodes.InsufficientBalance);
            }

            return Result<WalletTransaction>.Failure(ex.Message, Result.ErrorCodes.DatabaseError);
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
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);
            var tx = await conn.QuerySingleOrDefaultAsync<WalletTransaction>(
                "dbo.usp_CreditWallet",
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

            if (tx == null)
            {
                return Result<WalletTransaction>.Failure("Wallet credit failed.", Result.ErrorCodes.DatabaseError);
            }

            return Result<WalletTransaction>.Success(tx);
        }
        catch (Exception ex)
        {
            return Result<WalletTransaction>.Failure(ex.Message, Result.ErrorCodes.DatabaseError);
        }
    }

    public async Task<IReadOnlyList<WalletTransaction>> GetTransactionHistoryAsync(
        long userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var result = await conn.QueryAsync<WalletTransaction>(
            "dbo.usp_GetWalletTransactions",
            new
            {
                UserId = userId,
                Page = page < 1 ? 1 : page,
                PageSize = pageSize < 1 ? 20 : pageSize
            },
            commandType: CommandType.StoredProcedure);

        return result.ToList();
    }

    public async Task<int> GetTransactionCountAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT COUNT(1) FROM dbo.WalletTransactions WHERE UserId = @UserId AND IsDeleted = 0";
        return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<bool> IsIdempotencyKeyProcessedAsync(string key, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT COUNT(1) FROM dbo.WalletTransactions WHERE IdempotencyKey = @Key";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { Key = key });
        return count > 0;
    }
}
