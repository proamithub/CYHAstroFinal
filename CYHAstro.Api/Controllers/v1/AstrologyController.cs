using CYHAstro.Core.DTOs.Astrology;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class AstrologyController : ControllerBase
{
    private readonly IAstrologyCalculationService _astrologyService;
    private readonly ILogger<AstrologyController> _logger;

    public AstrologyController(
        IAstrologyCalculationService astrologyService,
        ILogger<AstrologyController> logger)
    {
        _astrologyService = astrologyService;
        _logger = logger;
    }

    /// <summary>
    /// Get the daily horoscope forecast for a specific zodiac sign (1 = Aries, 12 = Pisces).
    /// Grounded in planetary aspects and enhanced with Google Gemini AI.
    /// </summary>
    [HttpGet("daily-horoscope")]
    [ProducesResponseType(typeof(DailyHoroscopeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDailyHoroscope(
        [FromQuery] int sign,
        [FromQuery] string? date = null,
        [FromQuery] string language = "en",
        CancellationToken ct = default)
    {
        DateOnly? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var d))
        {
            parsedDate = d;
        }

        var result = await _astrologyService.GetDailyHoroscopeAsync(sign, parsedDate, language, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Compute a complete Vedic Birth Chart (Kundli) including Lagna, Moon Sign, Nakshatra,
    /// 9 planetary longitudes, 12 Bhavas (houses), Mangal Dosha check, and Gemini AI synthesis.
    /// </summary>
    [HttpPost("kundli")]
    [ProducesResponseType(typeof(KundliChartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateKundli(
        [FromBody] KundliRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required." });
        }

        var result = await _astrologyService.GenerateKundliAsync(request, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Perform classical 36 Gunas Vedic Matchmaking (Ashtakoot Milan) between bride and groom.
    /// Computes Varna, Vashya, Tara, Yoni, Graha Maitri, Gana, Bhakoot, and Nadi with Gemini AI verdict.
    /// </summary>
    [HttpPost("matchmaking")]
    [ProducesResponseType(typeof(MatchmakingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateMatchmaking(
        [FromBody] MatchmakingRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.MaleName) || string.IsNullOrWhiteSpace(request.FemaleName))
        {
            return BadRequest(new { error = "Both groom and bride names are required." });
        }

        var result = await _astrologyService.CalculateMatchmakingAsync(request, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }
}
