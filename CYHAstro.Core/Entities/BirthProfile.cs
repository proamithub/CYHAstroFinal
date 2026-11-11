namespace CYHAstro.Core.Entities;

/// <summary>
/// Birth profile for Kundli calculations.
/// A user can have multiple birth profiles (self + family).
/// </summary>
public sealed class BirthProfile : BaseEntity
{
    public long UserId { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty; // M, F, O
    public DateOnly DateOfBirth { get; set; }
    public TimeOnly? TimeOfBirth { get; set; }
    public bool IsTimeApproximate { get; set; }
    public string BirthPlace { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string TimeZoneId { get; set; } = string.Empty; // IANA timezone
    public int UtcOffsetMinutes { get; set; }
    public bool IsPrimary { get; set; }
    public string RelationLabel { get; set; } = "Self"; // Self, Spouse, Child, etc.
}
