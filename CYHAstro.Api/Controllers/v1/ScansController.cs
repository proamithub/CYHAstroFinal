using System.Security.Claims;
using CYHAstro.Core.DTOs.Scans;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class ScansController : ControllerBase
{
    private readonly IAstroScanService _scanService;
    private readonly ILogger<ScansController> _logger;

    public ScansController(IAstroScanService scanService, ILogger<ScansController> logger)
    {
        _scanService = scanService;
        _logger = logger;
    }

    /// <summary>
    /// Gets public scan pricing and availability status.
    /// </summary>
    [HttpGet("settings")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AstroScanSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var result = await _scanService.GetPublicSettingsAsync(ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error }));
    }

    /// <summary>
    /// Process a Palm Scan (हस्तरेखा विश्लेषण) with Gemini Vision AI.
    /// Accepts multipart/form-data with image file.
    /// </summary>
    [HttpPost("palm")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AstroScanDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ScanPalm(
        [FromForm] IFormFile image,
        [FromForm] string handType = "RIGHT",
        [FromForm] long? birthProfileId = null,
        [FromForm] string language = "hi",
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        if (image == null || image.Length == 0)
        {
            return BadRequest(new { error = "कृपया हथेली की स्पष्ट फोटो अपलोड करें।" });
        }

        if (image.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { error = "फोटो का आकार 10MB से अधिक नहीं होना चाहिए।" });
        }

        using var stream = image.OpenReadStream();
        var result = await _scanService.ProcessPalmScanAsync(
            userId,
            stream,
            image.FileName,
            image.ContentType,
            handType,
            birthProfileId,
            language,
            ct);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error }));
    }

    /// <summary>
    /// Process a Head / Face Scan (सामुद्रिक मस्तक लक्षण विज्ञान) with Gemini Vision AI.
    /// Accepts multipart/form-data with image file.
    /// </summary>
    [HttpPost("head")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AstroScanDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ScanHead(
        [FromForm] IFormFile image,
        [FromForm] long? birthProfileId = null,
        [FromForm] string language = "hi",
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        if (image == null || image.Length == 0)
        {
            return BadRequest(new { error = "कृपया चेहरे/माथे की स्पष्ट फोटो अपलोड करें।" });
        }

        if (image.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { error = "फोटो का आकार 10MB से अधिक नहीं होना चाहिए।" });
        }

        using var stream = image.OpenReadStream();
        var result = await _scanService.ProcessHeadScanAsync(
            userId,
            stream,
            image.FileName,
            image.ContentType,
            birthProfileId,
            language,
            ct);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error }));
    }

    /// <summary>
    /// Get the history of all scans performed by the authenticated user.
    /// </summary>
    [HttpGet("history")]
    [Authorize]
    [ProducesResponseType(typeof(List<AstroScanListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _scanService.GetUserScanHistoryAsync(userId, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error }));
    }

    /// <summary>
    /// Get the full astrological scan report by PublicId.
    /// </summary>
    [HttpGet("{publicId}")]
    [Authorize]
    [ProducesResponseType(typeof(AstroScanDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] string publicId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _scanService.GetUserScanAsync(userId, publicId, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => NotFound(new { error }));
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}
