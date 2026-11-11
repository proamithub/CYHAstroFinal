using System.Security.Claims;
using CYHAstro.Core.DTOs.User;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Api.Controllers.v1;

[ApiController]
[Route("api/v1/birth-profiles")]
[Authorize]
public sealed class BirthProfilesController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ILogger<BirthProfilesController> _logger;

    public BirthProfilesController(IProfileService profileService, ILogger<BirthProfilesController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// List all saved birth profiles for the current authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BirthProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _profileService.GetBirthProfilesAsync(userId, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Get a specific birth profile by its Id.
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(BirthProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _profileService.GetBirthProfileByIdAsync(id, userId, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => NotFound(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Add a new birth profile (e.g. self, spouse, child, friend).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BirthProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateBirthProfileDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required." });
        }

        var result = await _profileService.CreateBirthProfileAsync(userId, request, ct);
        return result.Match<IActionResult>(
            success => CreatedAtAction(nameof(GetById), new { id = success.Id }, success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Update an existing birth profile.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(BirthProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] long id, [FromBody] UpdateBirthProfileDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _profileService.UpdateBirthProfileAsync(id, userId, request, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => NotFound(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Delete a birth profile (Soft delete).
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _profileService.DeleteBirthProfileAsync(id, userId, ct);
        return result.Match<IActionResult>(
            () => NoContent(),
            error => NotFound(new { error, code = result.ErrorCode }));
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}
