using CYHAstro.Core.DTOs.Auth;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Core.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace CYHAstro.Api.Controllers.v1;

/// <summary>
/// Authentication and session lifecycle management controller.
/// Rate-limited to protect against credential stuffing and brute force attacks.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account with email and password.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken ct)
    {
        var validator = new RegisterRequestValidator();
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                type = "https://httpstatuses.com/400",
                title = "Validation Failed",
                status = 400,
                errors = validationResult.Errors.Select(e => e.ErrorMessage)
            });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.RegisterAsync(request, ip, userAgent, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => result.ErrorCode switch
            {
                "CONFLICT" => Conflict(new { error, code = result.ErrorCode }),
                _ => BadRequest(new { error, code = result.ErrorCode })
            });
    }

    /// <summary>
    /// Sign in with email and password.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var validator = new LoginRequestValidator();
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                type = "https://httpstatuses.com/400",
                title = "Validation Failed",
                status = 400,
                errors = validationResult.Errors.Select(e => e.ErrorMessage)
            });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(request, ip, userAgent, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => result.ErrorCode switch
            {
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new { error, code = result.ErrorCode }),
                _ => Unauthorized(new { error, code = result.ErrorCode })
            });
    }

    /// <summary>
    /// Authenticate or register using a verified Google OAuth ID Token.
    /// </summary>
    [HttpPost("google-login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return BadRequest(new { error = "Google IdToken is required." });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.GoogleLoginAsync(request, ip, userAgent, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => Unauthorized(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Refresh access token and rotate refresh token.
    /// </summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken) || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { error = "AccessToken and RefreshToken are required." });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.RefreshTokenAsync(request, ip, userAgent, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => result.ErrorCode switch
            {
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new { error, code = result.ErrorCode }),
                _ => Unauthorized(new { error, code = result.ErrorCode })
            });
    }

    /// <summary>
    /// Revoke current session refresh token (Sign out).
    /// </summary>
    [HttpPost("revoke-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { error = "RefreshToken is required." });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _authService.RevokeTokenAsync(request.RefreshToken, ip, ct);
        return Ok(new { message = "Token successfully revoked." });
    }

    /// <summary>
    /// Sign out from all devices by revoking all active refresh tokens for the user.
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue("userId");
        if (!long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        await _authService.RevokeAllUserTokensAsync(userId, ct);
        return Ok(new { message = "All active sessions have been terminated." });
    }

    /// <summary>
    /// Get authenticated user details and active balance.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue("userId");
        if (!long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var result = await _authService.GetCurrentUserAsync(userId, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => NotFound(new { error, code = result.ErrorCode }));
    }
}
