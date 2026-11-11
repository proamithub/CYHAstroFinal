using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Auth;

namespace CYHAstro.Core.Interfaces.Services;

/// <summary>
/// High-level authentication and account management operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user with email and secure password.
    /// </summary>
    Task<Result<TokenResponseDto>> RegisterAsync(
        RegisterRequestDto request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default);

    /// <summary>
    /// Authenticate user via email and password with lockout protection.
    /// </summary>
    Task<Result<TokenResponseDto>> LoginAsync(
        LoginRequestDto request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default);

    /// <summary>
    /// Authenticate or register a user using verified Google OAuth ID Token.
    /// </summary>
    Task<Result<TokenResponseDto>> GoogleLoginAsync(
        GoogleLoginRequestDto request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default);

    /// <summary>
    /// Refresh access token and rotate refresh token.
    /// Revokes token lineage if an already-used or revoked token is reused.
    /// </summary>
    Task<Result<TokenResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default);

    /// <summary>
    /// Revoke a specific refresh token (Sign out).
    /// </summary>
    Task<Result> RevokeTokenAsync(
        string refreshToken,
        string? ipAddress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Revoke all refresh tokens for a user (Sign out from all devices).
    /// </summary>
    Task<Result> RevokeAllUserTokensAsync(
        long userId,
        CancellationToken ct = default);

    /// <summary>
    /// Get current authenticated user profile by user profile ID.
    /// </summary>
    Task<Result<UserProfileDto>> GetCurrentUserAsync(
        long userId,
        CancellationToken ct = default);
}
