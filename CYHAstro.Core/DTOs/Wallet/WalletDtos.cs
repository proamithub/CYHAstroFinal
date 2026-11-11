namespace CYHAstro.Core.DTOs.Wallet;

public sealed record WalletBalanceDto
{
    public long UserId { get; init; }
    public decimal PaidBalance { get; init; }
    public decimal BonusBalance { get; init; }
    public decimal TotalBalance => PaidBalance + BonusBalance;
    public string CurrencyCode { get; init; } = "INR";
    public long RowVersion { get; init; }
}

public sealed record WalletTransactionDto
{
    public long Id { get; init; }
    public long WalletId { get; init; }
    public long UserId { get; init; }
    public string TransactionType { get; init; } = string.Empty; // CREDIT, DEBIT, BONUS, REFUND
    public string BalanceType { get; init; } = string.Empty; // PAID, BONUS
    public decimal Amount { get; init; }
    public decimal BalanceBefore { get; init; }
    public decimal BalanceAfter { get; init; }
    public string ReferenceType { get; init; } = string.Empty;
    public long? ReferenceId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string IdempotencyKey { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed record RechargePackageDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal BonusPercentage { get; init; }
    public decimal BonusAmount { get; init; }
    public decimal TotalCredit => Amount + BonusAmount;
    public bool IsPopular { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed record CreateRechargeOrderRequestDto
{
    public long PackageId { get; init; }
    public string PaymentGateway { get; init; } = "Razorpay"; // Razorpay, Cashfree, Mock
    public string? ReturnUrl { get; init; }
}

public sealed record CreateRechargeOrderResponseDto
{
    public string PaymentPublicId { get; init; } = string.Empty;
    public string GatewayOrderId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "INR";
    public string PaymentGateway { get; init; } = "Razorpay";
    public string? GatewayKey { get; init; }
}

public sealed record VerifyPaymentRequestDto
{
    public string PaymentPublicId { get; init; } = string.Empty;
    public string GatewayOrderId { get; init; } = string.Empty;
    public string GatewayPaymentId { get; init; } = string.Empty;
    public string GatewaySignature { get; init; } = string.Empty;
}

public sealed record VerifyPaymentResponseDto
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public decimal NewTotalBalance { get; init; }
    public decimal CreditedAmount { get; init; }
    public decimal BonusCredited { get; init; }
}
