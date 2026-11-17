using System.Text.Json.Serialization;

namespace CYHAstro.Core.DTOs.Scans;

/// <summary>
/// Detailed breakdown of an individual detected line, mount, or facial feature.
/// </summary>
public sealed class ScanFeatureItemDto
{
    public string Name { get; set; } = string.Empty;              // e.g. "जीवन रेखा (Life Line)"
    public string SanskritName { get; set; } = string.Empty;      // e.g. "आयुष्य रेखा"
    public string Status { get; set; } = string.Empty;            // e.g. "स्पष्ट एवं सुदृढ़ (Clear & Strong)"
    public string Significance { get; set; } = string.Empty;      // Astrological meaning
    public string Details { get; set; } = string.Empty;           // Specific reading for this user
    public int StrengthScore { get; set; } = 85;                  // 1-100 score
    public string? AuspiciousSign { get; set; }                   // e.g. "त्रिशूल (Trishul)", "मत्स्य (Fish)"
}

/// <summary>
/// Domain-specific predictions (Career, Health, Love, Wealth).
/// </summary>
public sealed class ScanDomainPredictionDto
{
    public string Category { get; set; } = string.Empty;          // "Career", "Health", "Marriage", "Wealth"
    public string HindiTitle { get; set; } = string.Empty;        // "करियर एवं धन", "स्वास्थ्य"
    public string Prediction { get; set; } = string.Empty;        // Detailed forecast
    public string AuspiciousAgeRange { get; set; } = string.Empty;// e.g. "28-36 वर्ष"
    public int FavorabilityScore { get; set; } = 80;              // 1-100
}

/// <summary>
/// Specific Vedic remedy recommended based on palm or face lines.
/// </summary>
public sealed class ScanRemedyDto
{
    public string Title { get; set; } = string.Empty;             // e.g. "सूर्य देव को अर्घ्य एवं गायत्री मंत्र"
    public string Description { get; set; } = string.Empty;       // How to perform it
    public string RecommendedGemstone { get; set; } = string.Empty;// e.g. "माणिक्य (Ruby) या पंचमुखी रुद्राक्ष"
    public string Mantra { get; set; } = string.Empty;            // e.g. "ॐ सूर्याय नमः"
    public string DaanDonation { get; set; } = string.Empty;      // e.g. "रविवार को गुड़ और तांबे का दान"
}

/// <summary>
/// Complete structured report returned by AI and stored in database.
/// </summary>
public sealed class AstroScanReportDto
{
    public string ScanType { get; set; } = "PALM";                // "PALM" or "HEAD"
    public string Title { get; set; } = string.Empty;
    public string OverviewSummary { get; set; } = string.Empty;
    public string HandOrFaceType { get; set; } = string.Empty;    // e.g. "पृथ्वी तत्व हथेली (Earth Hand)" / "चौड़ा मस्तक (Broad Forehead)"
    public int OverallAuspiciousnessScore { get; set; } = 85;     // 1-100
    
    // Core features (Lines, Mounts, Forehead Planetary Lines)
    public List<ScanFeatureItemDto> PrimaryFeatures { get; set; } = new();
    public List<ScanFeatureItemDto> SecondaryFeatures { get; set; } = new();

    // Predictions
    public List<ScanDomainPredictionDto> LifeAspects { get; set; } = new();

    // Kundli Correlation (if birth chart is linked)
    public string? KundliCorrelationAnalysis { get; set; }

    // Vedic Remedies & Gemstone Recommendations
    public List<ScanRemedyDto> VedicRemedies { get; set; } = new();

    // Key Highlights / Bullet points for quick reading
    public List<string> KeyHighlights { get; set; } = new();
}

/// <summary>
/// User Request to process a scan.
/// </summary>
public sealed class CreateScanRequestDto
{
    public string ScanType { get; set; } = "PALM";                // "PALM" or "HEAD"
    public string? HandType { get; set; } = "RIGHT";              // "RIGHT" or "LEFT"
    public long? BirthProfileId { get; set; }
    public string Language { get; set; } = "hi";
    public string? Base64Image { get; set; }
    public string? MimeType { get; set; } = "image/jpeg";
}

/// <summary>
/// Scan detail view for user and admin.
/// </summary>
public sealed class AstroScanDetailDto
{
    public long Id { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? UserPhone { get; set; }
    public long? BirthProfileId { get; set; }
    public string? BirthProfileName { get; set; }
    public string ScanType { get; set; } = string.Empty;
    public string? HandType { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "COMPLETED";
    public decimal? ConfidenceScore { get; set; }
    public AstroScanReportDto Report { get; set; } = new();
    public string? AdminNotes { get; set; }
    public bool IsAdminReviewed { get; set; }
    public decimal CostCharged { get; set; }
    public int TokensUsed { get; set; }
    public string Language { get; set; } = "hi";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Summary item for user history list.
/// </summary>
public sealed class AstroScanListItemDto
{
    public long Id { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public string ScanType { get; set; } = string.Empty;
    public string? HandType { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int AuspiciousScore { get; set; }
    public decimal CostCharged { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAdminReviewed { get; set; }
}

/// <summary>
/// Admin configuration settings DTO.
/// </summary>
public sealed class AstroScanSettingsDto
{
    public int Id { get; set; }
    public bool IsPalmScanEnabled { get; set; } = true;
    public bool IsHeadScanEnabled { get; set; } = true;
    public decimal PalmScanPrice { get; set; } = 0.00m;
    public decimal HeadScanPrice { get; set; } = 0.00m;
    public int FreeScansPerUser { get; set; } = 1;
    public string GeminiModel { get; set; } = "gemini-1.5-flash";
    public string? PalmSystemPrompt { get; set; }
    public string? HeadSystemPrompt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Admin review submit model.
/// </summary>
public sealed class UpdateAdminReviewDto
{
    public string PublicId { get; set; } = string.Empty;
    public string AdminNotes { get; set; } = string.Empty;
}
