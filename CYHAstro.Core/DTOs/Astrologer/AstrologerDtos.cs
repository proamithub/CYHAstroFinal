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

public sealed record SubmitReviewRequestDto
{
    public string SessionPublicId { get; init; } = string.Empty;
    public int Rating { get; init; } // 1 - 5
    public string? ReviewText { get; init; }
}

public sealed record AstrologerDashboardDto
{
    public long AstrologerId { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? ProfilePhotoUrl { get; init; }
    public bool IsAvailable { get; init; }
    public decimal Rating { get; init; }
    public int TotalReviews { get; init; }
    public int TotalConsultations { get; init; }
    public decimal TodayEarnings { get; init; }
    public decimal TotalEarnings { get; init; }
    public decimal ChatRatePerMinute { get; init; }
    public decimal AudioRatePerMinute { get; init; }
    public decimal VideoRatePerMinute { get; init; }
    public int PendingRequestsCount { get; init; }
    public List<CYHAstro.Core.DTOs.Consultation.ConsultationResponseDto> RecentSessions { get; init; } = new();
    public List<CYHAstro.Core.DTOs.Consultation.ConsultationResponseDto> IncomingRequests { get; init; } = new();
}

public sealed record UpdateAstrologerProfileDto
{
    public string DisplayName { get; init; } = string.Empty;
    public string Bio { get; init; } = string.Empty;
    public string Specializations { get; init; } = string.Empty;
    public string Languages { get; init; } = string.Empty;
    public decimal ChatRatePerMinute { get; init; }
    public decimal AudioRatePerMinute { get; init; }
    public decimal VideoRatePerMinute { get; init; }
    public int ExperienceYears { get; init; }
    public string? ProfilePhotoUrl { get; init; }
}

public sealed record AstrologerEarningItemDto
{
    public long Id { get; init; }
    public string SessionPublicId { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public string SessionType { get; init; } = string.Empty;
    public int DurationSeconds { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal CommissionDeducted { get; init; }
    public decimal NetEarning { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record AstrologerRegistrationDto
{
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
    public int ExperienceYears { get; init; } = 5;
    public string Specializations { get; init; } = "Vedic Astrology, Kundli Analysis";
    public string Languages { get; init; } = "Hindi, English";
    public string? Bio { get; init; }
    public decimal ChatRatePerMinute { get; init; } = 15m;
    public decimal AudioRatePerMinute { get; init; } = 25m;
    public decimal VideoRatePerMinute { get; init; } = 35m;
}
