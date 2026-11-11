namespace CYHAstro.Core.DTOs.Auth;

/// <summary>
/// Registration request model.
/// </summary>
public sealed class RegisterRequestDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Standard email & password login request.
/// </summary>
public sealed class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
}

/// <summary>
/// Google OAuth 2.0 login request using client ID token.
/// </summary>
public sealed class GoogleLoginRequestDto
{
    public string IdToken { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
}

/// <summary>
/// Token refresh request using active refresh token.
/// </summary>
public sealed class RefreshTokenRequestDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Token revocation request (Logout).
/// </summary>
public sealed class RevokeTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Successful authentication response with JWT access token & rotating refresh token.
/// </summary>
public sealed class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public UserProfileDto User { get; set; } = null!;
}

/// <summary>
/// User profile summary returned in auth response.
/// </summary>
public sealed class UserProfileDto
{
    public long Id { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public decimal WalletBalance { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}
