using CYHAstro.Core.DTOs.Panchang;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Client.Controllers;

[Area("Client")]
public sealed class PanchangController : Controller
{
    private readonly IPanchangService _panchangService;
    private readonly ILogger<PanchangController> _logger;

    public PanchangController(IPanchangService panchangService, ILogger<PanchangController> logger)
    {
        _panchangService = panchangService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? location,
        decimal? lat,
        decimal? lon,
        string? date,
        CancellationToken ct)
    {
        var targetDate = DateTime.Today;
        if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsedDate))
        {
            targetDate = parsedDate.Date;
        }

        var request = new PanchangRequestDto
        {
            Date = targetDate,
            Latitude = lat ?? 28.6139m,
            Longitude = lon ?? 77.2090m,
            TimeZoneOffsetHours = 5.5m,
            LocationName = string.IsNullOrWhiteSpace(location) ? "New Delhi, India" : location
        };

        var result = await _panchangService.GetPanchangAsync(request, ct);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData["Error"] = result.Error ?? "Could not calculate Panchang.";
            return View(new PanchangResponseDto());
        }

        ViewBag.SelectedDate = targetDate.ToString("yyyy-MM-dd");
        ViewBag.SelectedCity = request.LocationName;
        ViewBag.Lat = request.Latitude;
        ViewBag.Lon = request.Longitude;

        return View(result.Value);
    }
}
