using CYHAstro.Core.DTOs.Scans;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
public sealed class ScansController : Controller
{
    private readonly IAstroScanService _scanService;
    private readonly ILogger<ScansController> _logger;

    public ScansController(IAstroScanService scanService, ILogger<ScansController> logger)
    {
        _scanService = scanService;
        _logger = logger;
    }

    /// <summary>
    /// Displays all user scans with search, filter by Palm/Head, and pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? scanType,
        string? search,
        int page = 1,
        int pageSize = 15,
        CancellationToken ct = default)
    {
        ViewBag.ScanType = scanType;
        ViewBag.Search = search;

        var result = await _scanService.AdminGetAllScansAsync(scanType, search, page, pageSize, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "Failed to load scans.";
            return View(new Core.Common.PagedResult<AstroScanDetailDto> { Page = 1, PageSize = pageSize });
        }

        return View(result.Value);
    }

    /// <summary>
    /// Displays full details of an individual scan, side-by-side with original photo and AI Vedic analysis.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var result = await _scanService.AdminGetScanDetailAsync(id, ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "Scan not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    /// <summary>
    /// Submits manual astrologer/admin review notes for a user's scan.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReviewNotes(string publicId, string adminNotes, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return BadRequest();

        var reviewerName = User.Identity?.Name ?? "Admin";
        var result = await _scanService.AdminUpdateReviewNotesAsync(publicId, adminNotes, reviewerName, ct);

        if (result.IsSuccess)
        {
            TempData["Success"] = "Astrologer review notes saved successfully and marked as reviewed.";
        }
        else
        {
            TempData["Error"] = result.Error ?? "Failed to save review notes.";
        }

        return RedirectToAction(nameof(Details), new { id = publicId });
    }

    /// <summary>
    /// Configuration page for Palm Scan and Head Scan settings, pricing, free limits, and prompts.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        var result = await _scanService.AdminGetSettingsAsync(ct);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "Failed to load scan settings.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Value);
    }

    /// <summary>
    /// Updates pricing, model, and system prompts.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSettings(AstroScanSettingsDto model, CancellationToken ct)
    {
        var adminName = User.Identity?.Name ?? "Admin";
        var result = await _scanService.AdminUpdateSettingsAsync(model, adminName, ct);

        if (result.IsSuccess)
        {
            TempData["Success"] = "Scan system settings, pricing, and AI prompts updated successfully.";
        }
        else
        {
            TempData["Error"] = result.Error ?? "Failed to update scan settings.";
        }

        return RedirectToAction(nameof(Settings));
    }
}
