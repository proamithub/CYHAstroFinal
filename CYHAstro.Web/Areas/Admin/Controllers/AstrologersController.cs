using CYHAstro.Core.DTOs.Admin;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
public sealed class AstrologersController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AstrologersController> _logger;

    public AstrologersController(IAdminService adminService, ILogger<AstrologersController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status, string? search, CancellationToken ct)
    {
        ViewBag.Status = status;
        ViewBag.Search = search;

        var res = await _adminService.GetAstrologersAsync(status, search, ct);
        var list = res.IsSuccess ? res.Value : new List<AdminAstrologerDto>();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken ct)
    {
        var res = await _adminService.GetAstrologerByIdAsync(id, ct);
        if (!res.IsSuccess)
        {
            TempData["Error"] = "Astrologer not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(res.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] AdminUpdateAstrologerDto model, CancellationToken ct)
    {
        var res = await _adminService.UpdateAstrologerStatusAsync(model, ct);
        if (res.IsSuccess)
        {
            TempData["Success"] = "Astrologer profile settings updated successfully.";
        }
        else
        {
            TempData["Error"] = res.Error ?? "Failed to update astrologer profile.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickToggleVerify(long id, bool isVerified, CancellationToken ct)
    {
        var astroRes = await _adminService.GetAstrologerByIdAsync(id, ct);
        if (!astroRes.IsSuccess)
        {
            TempData["Error"] = "Astrologer not found.";
            return RedirectToAction(nameof(Index));
        }

        var astro = astroRes.Value!;
        var update = new AdminUpdateAstrologerDto
        {
            Id = astro.Id,
            IsVerified = isVerified,
            IsActive = isVerified ? true : astro.IsActive,
            CommissionRatePct = astro.CommissionRatePct,
            ChatRatePerMinute = astro.ChatRatePerMinute,
            AudioRatePerMinute = astro.AudioRatePerMinute,
            VideoRatePerMinute = astro.VideoRatePerMinute
        };

        var res = await _adminService.UpdateAstrologerStatusAsync(update, ct);
        if (res.IsSuccess)
        {
            TempData["Success"] = $"Astrologer verification {(isVerified ? "approved" : "revoked")}.";
        }
        else
        {
            TempData["Error"] = res.Error ?? "Failed to toggle verification.";
        }

        return RedirectToAction(nameof(Index));
    }
}
