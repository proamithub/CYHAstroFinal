using System.Security.Claims;
using CYHAstro.Core.DTOs.Astrology;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Client.Controllers;

[Area("Client")]
public sealed class KundliController : Controller
{
    private readonly IAstrologyCalculationService _astrologyService;
    private readonly IProfileService _profileService;
    private readonly IKundliPdfService _pdfService;
    private readonly IMatchmakingPdfService _matchmakingPdfService;
    private readonly ILogger<KundliController> _logger;

    public KundliController(
        IAstrologyCalculationService astrologyService,
        IProfileService profileService,
        IKundliPdfService pdfService,
        IMatchmakingPdfService matchmakingPdfService,
        ILogger<KundliController> logger)
    {
        _astrologyService = astrologyService;
        _profileService = profileService;
        _pdfService = pdfService;
        _matchmakingPdfService = matchmakingPdfService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var model = new KundliRequestDto
        {
            DateOfBirth = DateTime.Today.AddYears(-25),
            TimeOfBirth = new TimeSpan(12, 0, 0),
            BirthPlace = "New Delhi, India",
            Latitude = 28.6139m,
            Longitude = 77.2090m
        };

        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (long.TryParse(userIdStr, out var userId))
            {
                var profilesRes = await _profileService.GetBirthProfilesAsync(userId, ct);
                if (profilesRes.IsSuccess && profilesRes.Value!.Any())
                {
                    var primary = profilesRes.Value!.FirstOrDefault(p => p.IsPrimary) ?? profilesRes.Value!.First();
                    model = new KundliRequestDto
                    {
                        Name = primary.Name,
                        Gender = primary.Gender,
                        DateOfBirth = primary.DateOfBirth,
                        TimeOfBirth = primary.TimeOfBirth ?? new TimeSpan(12, 0, 0),
                        BirthPlace = primary.BirthPlace,
                        Latitude = primary.Latitude,
                        Longitude = primary.Longitude
                    };
                }
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(KundliRequestDto model, CancellationToken ct)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["Error"] = "Please fill in all birth details.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _astrologyService.GenerateKundliAsync(model, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "Could not generate Kundli.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.KundliRequest = model;
        return View("Result", result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadPdf(KundliRequestDto model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["Error"] = "Name is required to generate Kundli PDF.";
            return RedirectToAction(nameof(Index));
        }

        var chartResult = await _astrologyService.GenerateKundliAsync(model, ct);
        if (!chartResult.IsSuccess || chartResult.Value == null)
        {
            TempData["Error"] = chartResult.Error ?? "Kundli calculation failed.";
            return RedirectToAction(nameof(Index));
        }

        var pdfResult = await _pdfService.GenerateKundliPdfAsync(chartResult.Value, model, ct);
        if (!pdfResult.IsSuccess || pdfResult.Value == null)
        {
            TempData["Error"] = pdfResult.Error ?? "PDF generation failed.";
            return RedirectToAction(nameof(Index));
        }

        var safeName = string.Join("_", model.Name.Split(Path.GetInvalidFileNameChars()));
        return File(pdfResult.Value, "application/pdf", $"CYHAstro_Kundli_{safeName}.pdf");
    }

    [HttpGet]
    public IActionResult Matchmaking()
    {
        var model = new MatchmakingRequestDto
        {
            MaleDob = DateTime.Today.AddYears(-27),
            MaleTob = new TimeSpan(10, 30, 0),
            MaleLat = 28.6139m,
            MaleLon = 77.2090m,

            FemaleDob = DateTime.Today.AddYears(-25),
            FemaleTob = new TimeSpan(14, 15, 0),
            FemaleLat = 19.0760m,
            FemaleLon = 72.8777m
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Matchmaking(MatchmakingRequestDto model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.MaleName) || string.IsNullOrWhiteSpace(model.FemaleName))
        {
            TempData["Error"] = "Please enter both Groom and Bride names.";
            return View(model);
        }

        var result = await _astrologyService.CalculateMatchmakingAsync(model, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "Matchmaking calculation failed.";
            return View(model);
        }

        ViewBag.MaleName = model.MaleName;
        ViewBag.FemaleName = model.FemaleName;
        ViewBag.MatchmakingRequest = model;

        return View("MatchmakingResult", result.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadMatchmakingPdf(MatchmakingRequestDto model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.MaleName) || string.IsNullOrWhiteSpace(model.FemaleName))
        {
            TempData["Error"] = "Names are required to generate Matchmaking PDF.";
            return RedirectToAction(nameof(Matchmaking));
        }

        var result = await _astrologyService.CalculateMatchmakingAsync(model, ct);
        if (!result.IsSuccess || result.Value == null)
        {
            TempData["Error"] = result.Error ?? "Matchmaking calculation failed.";
            return RedirectToAction(nameof(Matchmaking));
        }

        var pdfResult = await _matchmakingPdfService.GenerateMatchmakingPdfAsync(result.Value, model, ct);
        if (!pdfResult.IsSuccess || pdfResult.Value == null)
        {
            TempData["Error"] = pdfResult.Error ?? "Matchmaking PDF generation failed.";
            return RedirectToAction(nameof(Matchmaking));
        }

        var safeGroom = string.Join("_", model.MaleName.Split(Path.GetInvalidFileNameChars()));
        var safeBride = string.Join("_", model.FemaleName.Split(Path.GetInvalidFileNameChars()));
        return File(pdfResult.Value, "application/pdf", $"CYHAstro_Kundli_Milan_{safeGroom}_{safeBride}.pdf");
    }
}
