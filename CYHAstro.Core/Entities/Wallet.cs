namespace CYHAstro.Core.Entities;

/// <summary>
/// Immutable wallet ledger entry.
/// Wallet balance = SUM of all transactions for a user.
/// Never update or delete; only INSERT new rows.
/// </summary>
public sealed class WalletTransaction : BaseEntity
{
    public long WalletId { get; set; }
    public long UserId { get; set; }
    public string TransactionType { get; set; } = string.Empty; // CREDIT, DEBIT, BONUS, REFUND
    public string BalanceType { get; set; } = string.Empty;     // PAID, BONUS
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string ReferenceType { get; set; } = string.Empty;   // RECHARGE, CONSULTATION, REFUND
    public long? ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;  // Prevents duplicate credits
}

public sealed class Wallet : BaseEntity
{
    public long UserId { get; set; }
    public decimal PaidBalance { get; set; }
    public decimal BonusBalance { get; set; }
    public decimal TotalBalance => PaidBalance + BonusBalance;
    public string CurrencyCode { get; set; } = "INR";
    public long RowVersion { get; set; } // Optimistic concurrency
}
