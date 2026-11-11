using CYHAstro.Core.Common;
using CYHAstro.Core.Constants;
using CYHAstro.Core.DTOs.Auth;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Data;
using Dapper;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Security.Claims;

namespace CYHAstro.Infrastructure.Identity;

/// <summary>
/// Full enterprise authentication service.
/// Coordinates ASP.NET Core Identity, Dapper ledger integration,
/// cryptographic token rotation, and threat detection.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<CYHAstroIdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<CYHAstroIdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        IDbConnectionFactory db,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<TokenResponseDto>> RegisterAsync(
        RegisterRequestDto request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Result<TokenResponseDto>.Failure("An account with this email address already exists.", Result.ErrorCodes.Conflict);
        }

        var publicId = "usr_" + Guid.NewGuid().ToString("N")[..16];
        var identityUser = new CYHAstroIdentityUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true, // Set to true or implement verification workflow
            CreatedAt = DateTime.UtcNow
        };

        var identityResult = await _userManager.CreateAsync(identityUser, request.Password);
        if (!identityResult.Succeeded)
        {
            var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
            return Result<TokenResponseDto>.Failure(errors, Result.ErrorCodes.ValidationFailed);
        }

        // Assign default User role
        await EnsureRoleExistsAsync(AppConstants.Roles.User);
        await _userManager.AddToRoleAsync(identityUser, AppConstants.Roles.User);

        // Sync to UserProfiles and Wallets tables via Stored Procedure
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var userProfile = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "dbo.usp_RegisterOrLoginUser",
            new
            {
                IdentityId = identityUser.Id,
                Email = request.Email,
                DisplayName = request.DisplayName,
                PhoneNumber = request.PhoneNumber,
                AvatarUrl = (string?)null,
                PublicId = publicId
            },
            commandType: CommandType.StoredProcedure);

        long userProfileId = userProfile.Id;
        decimal balance = (decimal)(userProfile.PaidBalance + userProfile.BonusBalance);

        // Generate Tokens
        var roles = new[] { AppConstants.Roles.User };
        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(
            identityUser.Id, userProfileId, publicId, request.Email, request.DisplayName, roles);
        var refreshTokenString = _tokenService.GenerateRefreshTokenString();

        // Save Hashed Refresh Token
        await SaveRefreshTokenAsync(conn, userProfileId, refreshTokenString, ipAddress, userAgent);

        // Audit Log
        await RecordAuditLogAsync(conn, userProfileId, "USER_REGISTERED", "UserProfiles", userProfileId.ToString(), ipAddress, userAgent);

        var response = new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = expiresAt,
            User = new UserProfileDto
            {
                Id = userProfileId,
                PublicId = publicId,
                DisplayName = request.DisplayName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                WalletBalance = balance,
                Roles = roles
            }
        };

        return Result<TokenResponseDto>.Success(response);
    }

    public async Task<Result<TokenResponseDto>> LoginAsync(
        LoginRequestDto request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        var identityUser = await _userManager.FindByEmailAsync(request.Email);
        if (identityUser == null)
        {
            await RecordSecurityEventAsync(null, "LOGIN_FAILED", "WARNING", $"Login failed: Email {request.Email} not found.", ipAddress, userAgent);
            return Result<TokenResponseDto>.Failure("Invalid email or password.", Result.ErrorCodes.Unauthorized);
        }

        if (await _userManager.IsLockedOutAsync(identityUser))
        {
            await RecordSecurityEventAsync(null, "ACCOUNT_LOCKED", "CRITICAL", $"Locked out account {request.Email} attempted login.", ipAddress, userAgent);
            return Result<TokenResponseDto>.Failure("Account is temporarily locked due to multiple failed login attempts. Please try again later.", Result.ErrorCodes.Forbidden);
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(identityUser, request.Password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(identityUser);
            await RecordSecurityEventAsync(null, "LOGIN_FAILED", "WARNING", $"Incorrect password for {request.Email}.", ipAddress, userAgent);
            return Result<TokenResponseDto>.Failure("Invalid email or password.", Result.ErrorCodes.Unauthorized);
        }

        // Reset failed count upon successful login
        await _userManager.ResetAccessFailedCountAsync(identityUser);

        // Fetch UserProfile & Wallet from database
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var profile = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM dbo.UserProfiles WHERE IdentityId = @IdentityId AND IsDeleted = 0",
            new { IdentityId = identityUser.Id });

        if (profile == null)
        {
            return Result<TokenResponseDto>.Failure("User profile record not found.", Result.ErrorCodes.NotFound);
        }

        long userProfileId = profile.Id;
        string publicId = profile.PublicId;
        string displayName = profile.DisplayName;

        var wallet = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT (PaidBalance + BonusBalance) AS TotalBalance FROM dbo.Wallets WHERE UserId = @UserId AND IsDeleted = 0",
            new { UserId = userProfileId });
        decimal balance = wallet != null ? (decimal)wallet.TotalBalance : 0.00m;

        var roles = await _userManager.GetRolesAsync(identityUser);

        // Generate Tokens
        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(
            identityUser.Id, userProfileId, publicId, identityUser.Email!, displayName, roles);
        var refreshTokenString = _tokenService.GenerateRefreshTokenString();

        // Save Hashed Refresh Token
        await SaveRefreshTokenAsync(conn, userProfileId, refreshTokenString, ipAddress, userAgent);

        // Update LastLoginAt
        await conn.ExecuteAsync(
            "UPDATE dbo.UserProfiles SET LastLoginAt = GETUTCDATE() WHERE Id = @Id",
            new { Id = userProfileId });

        await RecordAuditLogAsync(conn, userProfileId, "USER_LOGIN_SUCCESS", "UserProfiles", userProfileId.ToString(), ipAddress, userAgent);

        var response = new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = expiresAt,
            User = new UserProfileDto
            {
                Id = userProfileId,
                PublicId = publicId,
                DisplayName = displayName,
                Email = identityUser.Email!,
                PhoneNumber = profile.PhoneNumber,
                AvatarUrl = profile.AvatarUrl,
                WalletBalance = balance,
                Roles = roles.ToList()
            }
        };

        return Result<TokenResponseDto>.Success(response);
    }

    public async Task<Result<TokenResponseDto>> GoogleLoginAsync(
        GoogleLoginRequestDto request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var googleClientId = _configuration["Google:ClientId"];
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings();
            if (!string.IsNullOrEmpty(googleClientId) && googleClientId != "REPLACE_WITH_GOOGLE_CLIENT_ID")
            {
                validationSettings.Audience = new[] { googleClientId };
            }

            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, validationSettings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid Google ID token supplied from IP {IpAddress}", ipAddress);
            return Result<TokenResponseDto>.Failure("Invalid Google ID Token.", Result.ErrorCodes.Unauthorized);
        }

        var email = payload.Email;
        var name = payload.Name ?? payload.Email.Split('@')[0];
        var picture = payload.Picture;

        var identityUser = await _userManager.FindByEmailAsync(email);
        if (identityUser == null)
        {
            identityUser = new CYHAstroIdentityUser
            {
                UserName = email,
                Email = email,
                DisplayName = name,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(identityUser);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return Result<TokenResponseDto>.Failure(errors, Result.ErrorCodes.ValidationFailed);
            }

            await EnsureRoleExistsAsync(AppConstants.Roles.User);
            await _userManager.AddToRoleAsync(identityUser, AppConstants.Roles.User);
        }

        // Sync or get UserProfile
        var publicId = "usr_" + Guid.NewGuid().ToString("N")[..16];
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var userProfile = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "dbo.usp_RegisterOrLoginUser",
            new
            {
                IdentityId = identityUser.Id,
                Email = email,
                DisplayName = name,
                PhoneNumber = (string?)null,
                AvatarUrl = picture,
                PublicId = publicId
            },
            commandType: CommandType.StoredProcedure);

        long userProfileId = userProfile.Id;
        string resolvedPublicId = userProfile.PublicId;
        decimal balance = (decimal)(userProfile.PaidBalance + userProfile.BonusBalance);

        var roles = await _userManager.GetRolesAsync(identityUser);

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(
            identityUser.Id, userProfileId, resolvedPublicId, email, name, roles);
        var refreshTokenString = _tokenService.GenerateRefreshTokenString();

        await SaveRefreshTokenAsync(conn, userProfileId, refreshTokenString, ipAddress, userAgent);

        var response = new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = expiresAt,
            User = new UserProfileDto
            {
                Id = userProfileId,
                PublicId = resolvedPublicId,
                DisplayName = name,
                Email = email,
                AvatarUrl = picture,
                WalletBalance = balance,
                Roles = roles.ToList()
            }
        };

        return Result<TokenResponseDto>.Success(response);
    }

    public async Task<Result<TokenResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        var principal = _tokenService.GetPrincipalFromToken(request.AccessToken, validateLifetime: false);
        if (principal == null)
        {
            return Result<TokenResponseDto>.Failure("Invalid access token format.", Result.ErrorCodes.Unauthorized);
        }

        var identityId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        var userIdStr = principal.FindFirstValue("userId");

        if (string.IsNullOrEmpty(identityId) || !long.TryParse(userIdStr, out var userId))
        {
            return Result<TokenResponseDto>.Failure("Access token claims are invalid.", Result.ErrorCodes.Unauthorized);
        }

        var tokenHash = _tokenService.HashToken(request.RefreshToken);

        using var conn = await _db.CreateOpenConnectionAsync(ct);

        // Fetch the refresh token from DB
        var tokenRecord = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM dbo.RefreshTokens WHERE TokenHash = @TokenHash AND UserId = @UserId",
            new { TokenHash = tokenHash, UserId = userId });

        if (tokenRecord == null)
        {
            return Result<TokenResponseDto>.Failure("Invalid refresh token.", Result.ErrorCodes.Unauthorized);
        }

        // THREAT DETECTION: If the token is already revoked, an adversary might be replaying it!
        if ((bool)tokenRecord.IsRevoked)
        {
            _logger.LogWarning("SECURITY ALERT: Refresh token reuse detected for User {UserId}. Revoking all sessions.", userId);

            // Revoke ALL tokens for this compromised account
            await conn.ExecuteAsync(
                "UPDATE dbo.RefreshTokens SET IsRevoked = 1, RevokedAt = GETUTCDATE() WHERE UserId = @UserId",
                new { UserId = userId });

            await RecordSecurityEventAsync(userId, "TOKEN_REUSE_DETECTED", "CRITICAL",
                "Refresh token reuse detected. All active sessions invalidated.", ipAddress, userAgent);

            return Result<TokenResponseDto>.Failure("Invalid session state. Please sign in again.", Result.ErrorCodes.Forbidden);
        }

        // Check if token has expired
        if ((DateTime)tokenRecord.ExpiresAt < DateTime.UtcNow)
        {
            await conn.ExecuteAsync(
                "UPDATE dbo.RefreshTokens SET IsRevoked = 1, RevokedAt = GETUTCDATE() WHERE Id = @Id",
                new { Id = (long)tokenRecord.Id });

            return Result<TokenResponseDto>.Failure("Session has expired. Please sign in again.", Result.ErrorCodes.SessionExpired);
        }

        // Valid refresh: Generate fresh tokens
        var identityUser = await _userManager.FindByIdAsync(identityId);
        if (identityUser == null)
        {
            return Result<TokenResponseDto>.Failure("User account no longer exists.", Result.ErrorCodes.NotFound);
        }

        var profile = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM dbo.UserProfiles WHERE Id = @Id AND IsDeleted = 0",
            new { Id = userId });

        var wallet = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT (PaidBalance + BonusBalance) AS TotalBalance FROM dbo.Wallets WHERE UserId = @UserId AND IsDeleted = 0",
            new { UserId = userId });
        decimal balance = wallet != null ? (decimal)wallet.TotalBalance : 0.00m;

        var roles = await _userManager.GetRolesAsync(identityUser);

        string publicId = (string)profile.PublicId;
        string displayName = (string)profile.DisplayName;
        string? phoneNumber = (string?)profile.PhoneNumber;
        string? avatarUrl = (string?)profile.AvatarUrl;

        var (newAccessToken, expiresAt) = _tokenService.GenerateAccessToken(
            identityId, userId, publicId, identityUser.Email!, displayName, roles);
        var newRefreshTokenString = _tokenService.GenerateRefreshTokenString();
        var newRefreshTokenHash = _tokenService.HashToken(newRefreshTokenString);

        // ROTATE: Invalidate current token and point to replacement
        await conn.ExecuteAsync(
            "UPDATE dbo.RefreshTokens SET IsRevoked = 1, RevokedAt = GETUTCDATE(), ReplacedByTokenHash = @NewHash WHERE Id = @Id",
            new { Id = (long)tokenRecord.Id, NewHash = newRefreshTokenHash });

        // Insert new active refresh token
        var newExpiry = DateTime.UtcNow.AddDays(AppConstants.Jwt.RefreshTokenExpiryDays);
        await conn.ExecuteAsync(
            @"INSERT INTO dbo.RefreshTokens (UserId, TokenHash, DeviceInfo, IpAddress, ExpiresAt, IsRevoked, CreatedAt)
              VALUES (@UserId, @TokenHash, @DeviceInfo, @IpAddress, @ExpiresAt, 0, GETUTCDATE())",
            new
            {
                UserId = userId,
                TokenHash = newRefreshTokenHash,
                DeviceInfo = userAgent,
                IpAddress = ipAddress,
                ExpiresAt = newExpiry
            });

        var response = new TokenResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenString,
            ExpiresAt = expiresAt,
            User = new UserProfileDto
            {
                Id = userId,
                PublicId = publicId,
                DisplayName = displayName,
                Email = identityUser.Email!,
                PhoneNumber = phoneNumber,
                AvatarUrl = avatarUrl,
                WalletBalance = balance,
                Roles = roles.ToList()
            }
        };

        return Result<TokenResponseDto>.Success(response);
    }

    public async Task<Result> RevokeTokenAsync(
        string refreshToken,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        var rows = await conn.ExecuteAsync(
            "UPDATE dbo.RefreshTokens SET IsRevoked = 1, RevokedAt = GETUTCDATE() WHERE TokenHash = @TokenHash",
            new { TokenHash = tokenHash });

        return rows > 0 ? Result.Success() : Result.Failure("Token not found.", Result.ErrorCodes.NotFound);
    }

    public async Task<Result> RevokeAllUserTokensAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE dbo.RefreshTokens SET IsRevoked = 1, RevokedAt = GETUTCDATE() WHERE UserId = @UserId",
            new { UserId = userId });

        return Result.Success();
    }

    public async Task<Result<UserProfileDto>> GetCurrentUserAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var user = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "dbo.usp_GetUserProfile",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure);

        if (user == null)
        {
            return Result<UserProfileDto>.Failure("User not found.", Result.ErrorCodes.NotFound);
        }

        var identityUser = await _userManager.FindByIdAsync((string)user.IdentityId);
        var roles = identityUser != null ? await _userManager.GetRolesAsync(identityUser) : (IList<string>)["User"];

        var dto = new UserProfileDto
        {
            Id = user.Id,
            PublicId = user.PublicId,
            DisplayName = user.DisplayName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            WalletBalance = (decimal)user.TotalBalance,
            Roles = roles.ToList()
        };

        return Result<UserProfileDto>.Success(dto);
    }

    private async Task SaveRefreshTokenAsync(
        IDbConnection conn,
        long userId,
        string refreshTokenString,
        string? ipAddress,
        string? userAgent)
    {
        var hash = _tokenService.HashToken(refreshTokenString);
        var expiresAt = DateTime.UtcNow.AddDays(AppConstants.Jwt.RefreshTokenExpiryDays);

        await conn.ExecuteAsync(
            @"INSERT INTO dbo.RefreshTokens (UserId, TokenHash, DeviceInfo, IpAddress, ExpiresAt, IsRevoked, CreatedAt)
              VALUES (@UserId, @TokenHash, @DeviceInfo, @IpAddress, @ExpiresAt, 0, GETUTCDATE())",
            new
            {
                UserId = userId,
                TokenHash = hash,
                DeviceInfo = userAgent,
                IpAddress = ipAddress,
                ExpiresAt = expiresAt
            });
    }

    private async Task EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private async Task RecordSecurityEventAsync(
        long? userId,
        string eventType,
        string severity,
        string description,
        string? ipAddress,
        string? userAgent)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync();
            await conn.ExecuteAsync(
                @"INSERT INTO dbo.SecurityEvents (UserId, EventType, Severity, Description, IpAddress, UserAgent, CreatedAt)
                  VALUES (@UserId, @EventType, @Severity, @Description, @IpAddress, @UserAgent, GETUTCDATE())",
                new { UserId = userId, EventType = eventType, Severity = severity, Description = description, IpAddress = ipAddress, UserAgent = userAgent });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write security event {EventType}", eventType);
        }
    }

    private async Task RecordAuditLogAsync(
        IDbConnection conn,
        long userId,
        string action,
        string entityType,
        string entityId,
        string? ipAddress,
        string? userAgent)
    {
        try
        {
            await conn.ExecuteAsync(
                "dbo.usp_SaveAuditLog",
                new
                {
                    UserId = userId,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    OldValues = (string?)null,
                    NewValues = (string?)null,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                },
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log {Action}", action);
        }
    }
}
