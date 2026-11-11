namespace CYHAstro.Core.Entities;

/// <summary>
/// Astrologer profile entity.
/// </summary>
public sealed class Astrologer : BaseEntity
{
    public string PublicId { get; set; } = string.Empty;
    public long UserId { get; set; } // Links to ApplicationUser
    public string DisplayName { get; set; } = string.Empty;
    public string Specializations { get; set; } = string.Empty; // JSON array
    public string Languages { get; set; } = string.Empty;       // JSON array
    public string Bio { get; set; } = string.Empty;
    public decimal ChatRatePerMinute { get; set; }
    public decimal AudioRatePerMinute { get; set; }
    public decimal VideoRatePerMinute { get; set; }
    public decimal Rating { get; set; }
    public int TotalConsultations { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ProfilePhotoUrl { get; set; }
    public int ExperienceYears { get; set; }
}
