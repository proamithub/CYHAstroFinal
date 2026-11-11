namespace CYHAstro.Core.DTOs.User;

public sealed record UserProfileUpdateDto
{
    public string DisplayName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Gender { get; init; } // M, F, O
    public string? AvatarUrl { get; init; }
    public string PreferredLanguage { get; init; } = "en";
}

public sealed record BirthProfileDto
{
    public long Id { get; init; }
    public string PublicId { get; init; } = string.Empty;
    public long UserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Gender { get; init; } = "M";
    public DateTime DateOfBirth { get; init; }
    public TimeSpan? TimeOfBirth { get; init; }
    public bool IsTimeApproximate { get; init; }
    public string BirthPlace { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public string TimeZoneId { get; init; } = "Asia/Kolkata";
    public int UtcOffsetMinutes { get; init; } = 330;
    public bool IsPrimary { get; init; }
    public string RelationLabel { get; init; } = "Self";
    public DateTime CreatedAt { get; init; }
}

public sealed record CreateBirthProfileDto
{
    public string Name { get; init; } = string.Empty;
    public string Gender { get; init; } = "M"; // M, F, O
    public DateTime DateOfBirth { get; init; }
    public TimeSpan? TimeOfBirth { get; init; }
    public bool IsTimeApproximate { get; init; }
    public string BirthPlace { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public string TimeZoneId { get; init; } = "Asia/Kolkata";
    public int UtcOffsetMinutes { get; init; } = 330;
    public bool IsPrimary { get; init; }
    public string RelationLabel { get; init; } = "Self";
}

public sealed record UpdateBirthProfileDto
{
    public string Name { get; init; } = string.Empty;
    public string Gender { get; init; } = "M";
    public DateTime DateOfBirth { get; init; }
    public TimeSpan? TimeOfBirth { get; init; }
    public bool IsTimeApproximate { get; init; }
    public string BirthPlace { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public string TimeZoneId { get; init; } = "Asia/Kolkata";
    public int UtcOffsetMinutes { get; init; } = 330;
    public bool IsPrimary { get; init; }
    public string RelationLabel { get; init; } = "Self";
}

public sealed record UserPreferenceDto
{
    public bool ReceiveDailyNotification { get; init; } = true;
    public bool ReceivePromoAlerts { get; init; } = true;
    public string PreferredChartStyle { get; init; } = "NorthIndian";
    public string PreferredAyanamsha { get; init; } = "Lahiri";
    public string ThemeMode { get; init; } = "Dark";
}
