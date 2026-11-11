using System.Security.Claims;
using CYHAstro.Core.DTOs.User;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Client.Controllers;

[Authorize]
public sealed class DashboardController : Controller
{
    private readonly IProfileService _profileService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IProfileService profileService, ILogger<DashboardController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth");

        var profileRes = await _profileService.GetProfileAsync(userId, ct);
        var prefRes = await _profileService.GetPreferencesAsync(userId, ct);

        ViewBag.Preferences = prefRes.IsSuccess ? prefRes.Value : new UserPreferenceDto();
        return View(profileRes.IsSuccess ? profileRes.Value : null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([FromForm] UserProfileUpdateDto model, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth");

        var res = await _profileService.UpdateProfileAsync(userId, model, ct);
        if (res.IsSuccess)
        {
            TempData["Success"] = "Profile details updated successfully!";
        }
        else
        {
            TempData["Error"] = res.Error ?? "Failed to update profile.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePreferences([FromForm] UserPreferenceDto model, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth");

        var res = await _profileService.UpdatePreferencesAsync(userId, model, ct);
        if (res.IsSuccess)
        {
            TempData["Success"] = "Astrological preferences saved successfully!";
        }
        else
        {
            TempData["Error"] = res.Error ?? "Failed to update preferences.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> BirthProfiles(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth");

        var profilesRes = await _profileService.GetBirthProfilesAsync(userId, ct);
        var list = profilesRes.IsSuccess ? profilesRes.Value : new List<BirthProfileDto>();

        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBirthProfile([FromForm] CreateBirthProfileDto model, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth");

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            TempData["Error"] = "Name is required for the birth profile.";
            return RedirectToAction(nameof(BirthProfiles));
        }

        var res = await _profileService.CreateBirthProfileAsync(userId, model, ct);
        if (res.IsSuccess)
        {
            TempData["Success"] = $"Birth profile for {model.Name} created successfully!";
        }
        else
        {
            TempData["Error"] = res.Error ?? "Could not save birth profile.";
        }

        return RedirectToAction(nameof(BirthProfiles));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBirthProfile([FromForm] long profileId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth");

        var res = await _profileService.DeleteBirthProfileAsync(profileId, userId, ct);
        if (res.IsSuccess)
        {
            TempData["Success"] = "Birth profile deleted successfully.";
        }
        else
        {
            TempData["Error"] = res.Error ?? "Failed to delete birth profile.";
        }

        return RedirectToAction(nameof(BirthProfiles));
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}
