namespace CYHAstro.Core.Entities;

/// <summary>
/// Extends ASP.NET Identity user with CYHAstro-specific fields.
/// PublicId is a random opaque identifier exposed in URLs.
/// </summary>
public sealed class ApplicationUser : BaseEntity
{
    // Identity link (IdentityUser.Id = string GUID)
    public string IdentityId { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty; // Opaque URL-safe ID
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string PreferredLanguage { get; set; } = "en";
}
