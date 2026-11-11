namespace CYHAstro.Core.DTOs.Admin;

public sealed record AdminDashboardDto
{
    public long TotalUsers { get; init; }
    public int TotalAstrologers { get; init; }
    public int ActiveAstrologers { get; init; }
    public int PendingAstrologers { get; init; }
    public int TotalConsultations { get; init; }
    public decimal TotalPlatformRevenue { get; init; }
    public decimal TotalAstrologerPayouts { get; init; }
    public decimal TotalPlatformCommission { get; init; }
    public decimal TotalWalletLiability { get; init; }
    public IReadOnlyList<AdminTransactionDto> RecentTransactions { get; init; } = [];
    public IReadOnlyList<AdminConsultationDto> RecentConsultations { get; init; } = [];
}

public sealed record AdminAstrologerDto
{
    public long Id { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Specializations { get; init; } = string.Empty;
    public string Languages { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public decimal ChatRatePerMinute { get; init; }
    public decimal AudioRatePerMinute { get; init; }
    public decimal VideoRatePerMinute { get; init; }
    public decimal Rating { get; init; }
    public int TotalReviews { get; init; }
    public int TotalConsultations { get; init; }
    public int ExperienceYears { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsVerified { get; init; }
    public bool IsActive { get; init; }
    public decimal CommissionRatePct { get; init; }
    public string? ProfilePhotoUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record AdminUpdateAstrologerDto
{
    public long Id { get; init; }
    public bool IsVerified { get; init; }
    public bool IsActive { get; init; }
    public decimal CommissionRatePct { get; init; }
    public decimal ChatRatePerMinute { get; init; }
    public decimal AudioRatePerMinute { get; init; }
    public decimal VideoRatePerMinute { get; init; }
}

public sealed record AdminRechargePackageDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal BonusPercentage { get; init; }
    public decimal BonusAmount { get; init; }
    public bool IsPopular { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}

public sealed record AdminTransactionDto
{
    public long Id { get; init; }
    public long UserId { get; init; }
    public string UserDisplayName { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
    public string BalanceType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal BalanceBefore { get; init; }
    public decimal BalanceAfter { get; init; }
    public string ReferenceType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed record AdminConsultationDto
{
    public long Id { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string AstrologerName { get; init; } = string.Empty;
    public string SessionType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int DurationSeconds { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PlatformCommission { get; init; }
    public decimal AstrologerEarnings { get; init; }
    public DateTime CreatedAt { get; init; }
}
