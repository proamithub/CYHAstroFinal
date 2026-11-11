namespace CYHAstro.Core.DTOs.Astrologer;

public sealed record AstrologerListingDto
{
    public long Id { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public List<string> Specializations { get; init; } = new();
    public List<string> Languages { get; init; } = new();
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
    public string? ProfilePhotoUrl { get; init; }
}

public sealed record AstrologerDetailDto
{
    public long Id { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public List<string> Specializations { get; init; } = new();
    public List<string> Languages { get; init; } = new();
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
    public string? ProfilePhotoUrl { get; init; }
    public List<AstrologerReviewDto> RecentReviews { get; init; } = new();
}

public sealed record AstrologerReviewDto
{
    public long Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public decimal Rating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record AstrologerFilterRequestDto
{
    public string? Search { get; init; }
    public string? Specialization { get; init; }
    public string? Language { get; init; }
    public bool? IsAvailable { get; init; }
    public string SortBy { get; init; } = "RATING_DESC"; // RATING_DESC, PRICE_ASC, PRICE_DESC, EXPERIENCE_DESC
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
