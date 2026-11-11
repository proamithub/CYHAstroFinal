using System.Security.Claims;
using CYHAstro.Core.DTOs.Auth;
using CYHAstro.Core.DTOs.User;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IProfileService profileService, ILogger<UsersController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Get current authenticated user profile.
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _profileService.GetProfileAsync(userId, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => NotFound(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Update current authenticated user profile.
    /// </summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UserProfileUpdateDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _profileService.UpdateProfileAsync(userId, request, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Get current authenticated user preferences.
    /// </summary>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(UserPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _profileService.GetPreferencesAsync(userId, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Update user preferences (theme, notification settings, chart style).
    /// </summary>
    [HttpPut("preferences")]
    [ProducesResponseType(typeof(UserPreferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UserPreferenceDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _profileService.UpdatePreferencesAsync(userId, request, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}
