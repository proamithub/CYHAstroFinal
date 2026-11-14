namespace CYHAstro.Core.DTOs.Astrology;

public sealed record DailyHoroscopeDto
{
    public int ZodiacSignId { get; init; }
    public string ZodiacSignName { get; init; } = string.Empty;
    public string SanskritName { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public string GeneralText { get; init; } = string.Empty;
    public string? CareerText { get; init; }
    public string? LoveText { get; init; }
    public string? FinanceText { get; init; }
    public string? HealthText { get; init; }
    public int? LuckyNumber { get; init; }
    public string? LuckyColor { get; init; }
    public int? RatingPercentage { get; init; }
    public string LanguageCode { get; init; } = "en";
}

public sealed record KundliRequestDto
{
    public string Name { get; init; } = string.Empty;
    public string Gender { get; init; } = "M"; // M, F, O
    public DateTime DateOfBirth { get; init; }
    public TimeSpan TimeOfBirth { get; init; }
    public string BirthPlace { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public string TimeZoneId { get; init; } = "Asia/Kolkata";
    public string ChartStyle { get; init; } = "NorthIndian";
    public string Ayanamsha { get; init; } = "Lahiri";
    public bool IncludeAiInterpretation { get; init; } = true;
}

public sealed record KundliPlanetaryPositionDto
{
    public string PlanetName { get; init; } = string.Empty;
    public string SanskritName { get; init; } = string.Empty;
    public decimal Longitude { get; init; }
    public int SignId { get; init; }
    public string SignName { get; init; } = string.Empty;
    public int House { get; init; }
    public string Nakshatra { get; init; } = string.Empty;
    public int Pada { get; init; }
    public bool IsRetrograde { get; init; }
    public bool IsCombust { get; init; }
}

public sealed record KundliHouseDto
{
    public int HouseNumber { get; init; }
    public int SignId { get; init; }
    public string SignName { get; init; } = string.Empty;
    public decimal Degree { get; init; }
    public List<string> PlanetsInHouse { get; init; } = new();
}

public sealed record KundliYogaDto
{
    public string Name { get; init; } = string.Empty;
    public string SanskritName { get; init; } = string.Empty;
    public string Type { get; init; } = "Raj Yoga"; // Raj Yoga, Dhana Yoga, Auspicious, Inauspicious
    public string Description { get; init; } = string.Empty;
    public bool IsAuspicious { get; init; } = true;
    public string Impact { get; init; } = string.Empty;
}

public sealed record DashaPeriodDto
{
    public string PlanetName { get; init; } = string.Empty;
    public string SanskritName { get; init; } = string.Empty;
    public int StartYear { get; init; }
    public int EndYear { get; init; }
    public int DurationYears { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed record KundliChartDto
{
    public string PublicId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string AscendantSign { get; init; } = string.Empty;
    public int AscendantSignId { get; init; }
    public string MoonSign { get; init; } = string.Empty;
    public string SunSign { get; init; } = string.Empty;
    public string BirthNakshatra { get; init; } = string.Empty;
    public int BirthPada { get; init; }
    public List<KundliPlanetaryPositionDto> Planets { get; init; } = new();
    public List<KundliHouseDto> Houses { get; init; } = new();
    public List<KundliHouseDto> NavamshaHouses { get; init; } = new();
    public string MangalDoshaStatus { get; init; } = "No";
    public string KaalSarpDoshaStatus { get; init; } = "No";
    public string SadeSatiStatus { get; init; } = "None";
    public string CurrentDasha { get; init; } = string.Empty;
    public string? AiInterpretation { get; init; }

    // Life Scores (Speedometer / Gauges)
    public int CareerScore { get; init; } = 85;
    public int LoveScore { get; init; } = 75;
    public int HealthScore { get; init; } = 80;
    public int FortuneScore { get; init; } = 90;

    // Personalized Lucky Elements
    public string LuckyGemstone { get; init; } = string.Empty;
    public string LuckyColor { get; init; } = string.Empty;
    public string LuckyNumber { get; init; } = string.Empty;
    public string LuckyDirection { get; init; } = string.Empty;
    public string RulingDeity { get; init; } = string.Empty;
    public string RecommendedRudraksha { get; init; } = string.Empty;

    // Yogas & Dasha Timeline
    public List<KundliYogaDto> Yogas { get; init; } = new();
    public List<DashaPeriodDto> DashaTimeline { get; init; } = new();
}

public sealed record MatchmakingRequestDto
{
    public string MaleName { get; init; } = string.Empty;
    public DateTime MaleDob { get; init; }
    public TimeSpan MaleTob { get; init; }
    public string MalePlace { get; init; } = "New Delhi, India";
    public decimal MaleLat { get; init; } = 28.6139m;
    public decimal MaleLon { get; init; } = 77.2090m;

    public string FemaleName { get; init; } = string.Empty;
    public DateTime FemaleDob { get; init; }
    public TimeSpan FemaleTob { get; init; }
    public string FemalePlace { get; init; } = "Mumbai, India";
    public decimal FemaleLat { get; init; } = 19.0760m;
    public decimal FemaleLon { get; init; } = 72.8777m;
}

public sealed record GunaMilanItemDto
{
    public string GunaName { get; init; } = string.Empty;
    public decimal MaxPoints { get; init; }
    public decimal ObtainedPoints { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed record MatchmakingResultDto
{
    public decimal TotalScore { get; init; } // Out of 36
    public decimal MaxScore => 36.0m;
    public string Conclusion { get; init; } = string.Empty; // Highly Recommended, Moderate, Inauspicious
    public List<GunaMilanItemDto> GunaBreakdown { get; init; } = new();
    public bool HasNadiDosha { get; init; }
    public bool HasBhakootDosha { get; init; }
    public bool HasGanaDosha { get; init; }
    public string? AiVerdict { get; init; }

    // Side-by-Side Charts & Manglik Compatibility
    public KundliChartDto? MaleChart { get; init; }
    public KundliChartDto? FemaleChart { get; init; }
    public bool MaleManglik { get; init; }
    public bool FemaleManglik { get; init; }
    public string ManglikConclusion { get; init; } = string.Empty;
}
