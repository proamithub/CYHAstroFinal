using CYHAstro.Client.Models;
using CYHAstro.Core.DTOs.Astrologer;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Client.Controllers;

public sealed class HomeController : Controller
{
    private readonly IAstrologerService _astrologerService;
    private readonly IAstrologyCalculationService _astrologyService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IAstrologerService astrologerService,
        IAstrologyCalculationService astrologyService,
        ILogger<HomeController> logger)
    {
        _astrologerService = astrologerService;
        _astrologyService = astrologyService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var astroResult = await _astrologerService.GetAstrologersAsync(
            new AstrologerFilterRequestDto { Page = 1, PageSize = 6, SortBy = "RATING_DESC" }, ct);

        var horoscopeResult = await _astrologyService.GetDailyHoroscopeAsync(1, null, "en", ct);
        var specialtiesResult = await _astrologerService.GetSpecialtiesAsync(ct);

        var viewModel = new HomeIndexViewModel
        {
            TopAstrologers = astroResult.IsSuccess ? astroResult.Value!.Items : new List<AstrologerListingDto>(),
            FeaturedHoroscope = horoscopeResult.IsSuccess ? horoscopeResult.Value : null,
            Specialties = specialtiesResult.IsSuccess ? specialtiesResult.Value! : new List<string>()
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    }
}
