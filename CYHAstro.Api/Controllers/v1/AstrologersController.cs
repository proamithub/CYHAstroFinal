using System.Security.Claims;
using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Astrologer;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class AstrologersController : ControllerBase
{
    private readonly IAstrologerService _astrologerService;
    private readonly ILogger<AstrologersController> _logger;

    public AstrologersController(IAstrologerService astrologerService, ILogger<AstrologersController> logger)
    {
        _astrologerService = astrologerService;
        _logger = logger;
    }

    /// <summary>
    /// Search and filter astrologers with pagination.
    /// Supports filtering by specialization, language, availability, and sorting by rating/price/experience.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AstrologerListingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAstrologers([FromQuery] AstrologerFilterRequestDto filter, CancellationToken ct)
    {
        var result = await _astrologerService.GetAstrologersAsync(filter, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Get detailed astrologer profile including ratings, reviews, pricing, and bio.
    /// </summary>
    [HttpGet("{publicId}")]
    [ProducesResponseType(typeof(AstrologerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAstrologerDetail([FromRoute] string publicId, CancellationToken ct)
    {
        var result = await _astrologerService.GetAstrologerByPublicIdAsync(publicId, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => NotFound(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Get all available astrologer specializations (e.g. Vedic, Tarot, KP, Numerology).
    /// </summary>
    [HttpGet("specialties")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpecialties(CancellationToken ct)
    {
        var result = await _astrologerService.GetSpecialtiesAsync(ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Get all supported astrologer consultation languages.
    /// </summary>
    [HttpGet("languages")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLanguages(CancellationToken ct)
    {
        var result = await _astrologerService.GetLanguagesAsync(ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Toggle online/offline availability status for the authenticated astrologer.
    /// </summary>
    [HttpPut("availability")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetAvailability([FromBody] SetAvailabilityRequestDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _astrologerService.SetAvailabilityAsync(userId, request.IsAvailable, ct);
        return result.Match<IActionResult>(
            () => Ok(new { message = $"Availability status updated to {(request.IsAvailable ? "Online" : "Offline")}." }),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}

public sealed record SetAvailabilityRequestDto
{
    public bool IsAvailable { get; init; }
}
