using CYHAstro.Core.DTOs.Panchang;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class PanchangController : ControllerBase
{
    private readonly IPanchangService _panchangService;
    private readonly ILogger<PanchangController> _logger;

    public PanchangController(IPanchangService panchangService, ILogger<PanchangController> logger)
    {
        _panchangService = panchangService;
        _logger = logger;
    }

    /// <summary>
    /// Get the full Vedic Panchang for today or a specific date and coordinates.
    /// Includes Tithi, Nakshatra, Yoga, Karana, Vaar, Sun/Moon times, Rahu Kaal, Abhijit Muhurat, and Choghadiyas.
    /// </summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(PanchangResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTodayPanchang(
        [FromQuery] decimal? lat,
        [FromQuery] decimal? lon,
        [FromQuery] decimal? tz,
        [FromQuery] string? location,
        CancellationToken ct = default)
    {
        var request = new PanchangRequestDto
        {
            Date = DateTime.UtcNow.Date,
            Latitude = lat ?? 28.6139m, // New Delhi
            Longitude = lon ?? 77.2090m,
            TimeZoneOffsetHours = tz ?? 5.5m,
            LocationName = string.IsNullOrWhiteSpace(location) ? "New Delhi, India" : location
        };

        var result = await _panchangService.GetPanchangAsync(request, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Calculate complete Vedic Panchang for any given date and geographical coordinates.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PanchangResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPanchang([FromBody] PanchangRequestDto request, CancellationToken ct = default)
    {
        var result = await _panchangService.GetPanchangAsync(request, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Get Day and Night Choghadiya periods with auspiciousness ratings and active time windows.
    /// </summary>
    [HttpGet("choghadiya")]
    [ProducesResponseType(typeof(IReadOnlyList<ChoghadiyaPeriodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetChoghadiya(
        [FromQuery] string? date,
        [FromQuery] decimal? lat,
        [FromQuery] decimal? lon,
        [FromQuery] decimal? tz,
        CancellationToken ct = default)
    {
        var parsedDate = DateTime.UtcNow.Date;
        if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var d))
        {
            parsedDate = d.Date;
        }

        var result = await _panchangService.GetChoghadiyaAsync(
            parsedDate,
            lat ?? 28.6139m,
            lon ?? 77.2090m,
            tz ?? 5.5m,
            ct);

        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }
}
