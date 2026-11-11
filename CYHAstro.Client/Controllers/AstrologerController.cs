using System.Security.Claims;
using CYHAstro.Core.DTOs.Astrologer;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Client.Controllers;

public sealed class AstrologerController : Controller
{
    private readonly IAstrologerService _astrologerService;
    private readonly IWalletService _walletService;
    private readonly ILogger<AstrologerController> _logger;

    public AstrologerController(
        IAstrologerService astrologerService,
        IWalletService walletService,
        ILogger<AstrologerController> logger)
    {
        _astrologerService = astrologerService;
        _walletService = walletService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? search,
        [FromQuery] string? specialization,
        [FromQuery] string? language,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        var filter = new AstrologerFilterRequestDto
        {
            Search = search,
            Specialization = specialization,
            Language = language,
            SortBy = sortBy ?? "RATING_DESC",
            Page = page < 1 ? 1 : page,
            PageSize = 12
        };

        var result = await _astrologerService.GetAstrologersAsync(filter, ct);
        var specialties = await _astrologerService.GetSpecialtiesAsync(ct);
        var languages = await _astrologerService.GetLanguagesAsync(ct);

        ViewBag.Search = search;
        ViewBag.Specialization = specialization;
        ViewBag.Language = language;
        ViewBag.SortBy = sortBy ?? "RATING_DESC";
        ViewBag.Specialties = specialties.IsSuccess ? specialties.Value : new List<string>();
        ViewBag.Languages = languages.IsSuccess ? languages.Value : new List<string>();

        return View(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string id, string? action = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return RedirectToAction(nameof(Index));
        }

        var result = await _astrologerService.GetAstrologerByPublicIdAsync(id, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = "Astrologer not found.";
            return RedirectToAction(nameof(Index));
        }

        decimal userBalance = 0;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = User.FindFirstValue("userId");
            if (long.TryParse(userIdStr, out var userId))
            {
                var walletRes = await _walletService.GetBalanceAsync(userId, ct);
                if (walletRes.IsSuccess)
                {
                    userBalance = walletRes.Value!.TotalBalance;
                }
            }
        }

        ViewBag.UserBalance = userBalance;
        ViewBag.Action = action;

        return View(result.Value);
    }
}
