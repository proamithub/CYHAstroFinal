using System.Security.Claims;

namespace CYHAstro.Core.Interfaces.Services;

/// <summary>
/// Handles JWT access token generation, validation, and refresh token hashing.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate a signed JWT token containing user identity and role claims.
    /// </summary>
    (string Token, DateTime ExpiresAt) GenerateAccessToken(
        string identityId,
        long userProfileId,
        string publicId,
        string email,
        string displayName,
        IEnumerable<string> roles);

    /// <summary>
    /// Generate a cryptographically secure random refresh token string.
    /// </summary>
    string GenerateRefreshTokenString();

    /// <summary>
    /// Compute SHA-256 hash of a refresh token for secure database storage.
    /// </summary>
    string HashToken(string token);

    /// <summary>
    /// Extract ClaimsPrincipal from an expired or active JWT access token.
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = false);
}
