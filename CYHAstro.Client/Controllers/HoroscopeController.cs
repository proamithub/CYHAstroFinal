using CYHAstro.Core.DTOs.Astrology;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Client.Controllers;

public sealed class HoroscopeController : Controller
{
    private readonly IAstrologyCalculationService _astrologyService;
    private readonly ILogger<HoroscopeController> _logger;

    public HoroscopeController(
        IAstrologyCalculationService astrologyService,
        ILogger<HoroscopeController> logger)
    {
        _astrologyService = astrologyService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] int sign = 1, [FromQuery] string language = "en", CancellationToken ct = default)
    {
        if (sign < 1 || sign > 12) sign = 1;

        var result = await _astrologyService.GetDailyHoroscopeAsync(sign, null, language, ct);
        ViewBag.CurrentSign = sign;
        ViewBag.Language = language;

        return View(result.Value);
    }
}
