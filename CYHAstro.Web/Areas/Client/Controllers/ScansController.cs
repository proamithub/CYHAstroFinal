using System.Security.Claims;
using CYHAstro.Core.DTOs.Scans;
using CYHAstro.Core.DTOs.User;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Client.Controllers;

[Area("Client")]
public sealed class ScansController : Controller
{
    private readonly IAstroScanService _scanService;
    private readonly IProfileService _profileService;
    private readonly IWalletService _walletService;
    private readonly ILogger<ScansController> _logger;

    public ScansController(
        IAstroScanService scanService,
        IProfileService profileService,
        IWalletService walletService,
        ILogger<ScansController> logger)
    {
        _scanService = scanService;
        _profileService = profileService;
        _walletService = walletService;
        _logger = logger;
    }

    /// <summary>
    /// Hub page introducing Palm Scan (हस्तरेखा) & Head Scan (सामुद्रिक शास्त्र).
    /// Shows features, pricing, and user's previous scans.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var settingsRes = await _scanService.GetPublicSettingsAsync(ct);
        ViewBag.Settings = settingsRes.IsSuccess ? settingsRes.Value : new AstroScanSettingsDto();

        var recentScans = new List<AstroScanListItemDto>();
        var userId = GetCurrentUserId();
        if (userId > 0)
        {
            var historyRes = await _scanService.GetUserScanHistoryAsync(userId, ct);
            if (historyRes.IsSuccess)
            {
                recentScans = historyRes.Value;
            }
        }

        ViewBag.RecentScans = recentScans;
        return View();
    }

    /// <summary>
    /// Interactive Palm Scan camera capture and upload page.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Palm(CancellationToken ct)
    {
        var settingsRes = await _scanService.GetPublicSettingsAsync(ct);
        ViewBag.Settings = settingsRes.IsSuccess ? settingsRes.Value : new AstroScanSettingsDto();

        var profiles = new List<BirthProfileDto>();
        var userId = GetCurrentUserId();
        if (userId > 0)
        {
            var profilesRes = await _profileService.GetBirthProfilesAsync(userId, ct);
            if (profilesRes.IsSuccess && profilesRes.Value != null)
            {
                profiles = profilesRes.Value.ToList();
            }
        }
        ViewBag.BirthProfiles = profiles;

        return View();
    }

    /// <summary>
    /// Interactive Head / Face Scan camera capture and upload page.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Head(CancellationToken ct)
    {
        var settingsRes = await _scanService.GetPublicSettingsAsync(ct);
        ViewBag.Settings = settingsRes.IsSuccess ? settingsRes.Value : new AstroScanSettingsDto();

        var profiles = new List<BirthProfileDto>();
        var userId = GetCurrentUserId();
        if (userId > 0)
        {
            var profilesRes = await _profileService.GetBirthProfilesAsync(userId, ct);
            if (profilesRes.IsSuccess && profilesRes.Value != null)
            {
                profiles = profilesRes.Value.ToList();
            }
        }
        ViewBag.BirthProfiles = profiles;

        return View();
    }

    /// <summary>
    /// Processes uploaded Palm image with Gemini Vision AI.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyzePalm(
        IFormFile? scanPhoto,
        string handType = "RIGHT",
        long? birthProfileId = null,
        string language = "hi",
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth", new { returnUrl = "/Client/Scans/Palm" });

        if (scanPhoto == null || scanPhoto.Length == 0)
        {
            TempData["Error"] = "कृपया अपनी हथेली की स्पष्ट फोटो अपलोड करें।";
            return RedirectToAction(nameof(Palm));
        }

        using var stream = scanPhoto.OpenReadStream();
        var result = await _scanService.ProcessPalmScanAsync(
            userId,
            stream,
            scanPhoto.FileName,
            scanPhoto.ContentType,
            handType,
            birthProfileId,
            language,
            ct);

        if (!result.IsSuccess || result.Value == null)
        {
            TempData["Error"] = result.Error ?? "हथेली का विश्लेषण करने में त्रुटि हुई।";
            return RedirectToAction(nameof(Palm));
        }

        TempData["Success"] = "आपकी हस्तरेखा का वैदिक विश्लेषण सफलतापूर्वक पूरा हुआ!";
        return RedirectToAction(nameof(Report), new { id = result.Value.PublicId });
    }

    /// <summary>
    /// Processes uploaded Head/Face image with Gemini Vision AI.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnalyzeHead(
        IFormFile? scanPhoto,
        long? birthProfileId = null,
        string language = "hi",
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth", new { returnUrl = "/Client/Scans/Head" });

        if (scanPhoto == null || scanPhoto.Length == 0)
        {
            TempData["Error"] = "कृपया अपने चेहरे/माथे की स्पष्ट फोटो अपलोड करें।";
            return RedirectToAction(nameof(Head));
        }

        using var stream = scanPhoto.OpenReadStream();
        var result = await _scanService.ProcessHeadScanAsync(
            userId,
            stream,
            scanPhoto.FileName,
            scanPhoto.ContentType,
            birthProfileId,
            language,
            ct);

        if (!result.IsSuccess || result.Value == null)
        {
            TempData["Error"] = result.Error ?? "मस्तक का विश्लेषण करने में त्रुटि हुई।";
            return RedirectToAction(nameof(Head));
        }

        TempData["Success"] = "सामुद्रिक शास्त्र मस्तक विश्लेषण सफलतापूर्वक संपन्न हुआ!";
        return RedirectToAction(nameof(Report), new { id = result.Value.PublicId });
    }

    /// <summary>
    /// Displays high-fidelity interactive report for a specific scan.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Report(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var userId = GetCurrentUserId();
        var result = await _scanService.AdminGetScanDetailAsync(id, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = "स्कैन रिपोर्ट नहीं मिली।";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    /// <summary>
    /// Displays complete past scan history for the user.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> History(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth");

        var result = await _scanService.GetUserScanHistoryAsync(userId, ct);
        var list = result.IsSuccess ? result.Value : new List<AstroScanListItemDto>();

        return View(list);
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}
