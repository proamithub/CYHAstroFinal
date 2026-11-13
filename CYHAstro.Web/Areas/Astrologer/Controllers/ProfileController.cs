using System.Security.Claims;
using CYHAstro.Core.DTOs.Astrologer;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Astrologer.Controllers;

[Area("Astrologer")]
[Authorize(AuthenticationSchemes = "AstrologerCookie", Roles = "Astrologer")]
public sealed class ProfileController : Controller
{
    private readonly IAstrologerService _astrologerService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IAstrologerService astrologerService, ILogger<ProfileController> logger)
    {
        _astrologerService = astrologerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth", new { area = "Astrologer" });

        var astroRes = await _astrologerService.GetAstrologerByUserIdAsync(userId, ct);
        if (!astroRes.IsSuccess || astroRes.Value == null)
        {
            TempData["Error"] = "Astrologer profile not found.";
            return RedirectToAction("Index", "Home", new { area = "Astrologer" });
        }

        var a = astroRes.Value;
        var model = new UpdateAstrologerProfileDto
        {
            DisplayName = a.DisplayName,
            Bio = a.Bio ?? "",
            Specializations = string.Join(", ", a.Specializations),
            Languages = string.Join(", ", a.Languages),
            ChatRatePerMinute = a.ChatRatePerMinute,
            AudioRatePerMinute = a.AudioRatePerMinute,
            VideoRatePerMinute = a.VideoRatePerMinute,
            ExperienceYears = a.ExperienceYears,
            ProfilePhotoUrl = a.ProfilePhotoUrl
        };

        ViewBag.AstrologerListing = a;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateAstrologerProfileDto model, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth", new { area = "Astrologer" });

        if (model.ChatRatePerMinute <= 0 || model.AudioRatePerMinute <= 0 || model.VideoRatePerMinute <= 0)
        {
            TempData["Error"] = "Consultation rates must be greater than zero.";
            return RedirectToAction(nameof(Index));
        }

        var res = await _astrologerService.UpdateProfileAsync(userId, model, ct);
        if (res.IsSuccess)
        {
            TempData["Success"] = "Your astrologer profile and consultation rates have been successfully updated!";
        }
        else
        {
            TempData["Error"] = res.Error ?? "Could not update profile.";
        }

        return RedirectToAction(nameof(Index));
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}
