using CYHAstro.Core.DTOs.Admin;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
public sealed class PackagesController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<PackagesController> _logger;

    public PackagesController(IAdminService adminService, ILogger<PackagesController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var res = await _adminService.GetRechargePackagesAsync(ct);
        var list = res.IsSuccess ? res.Value : new List<AdminRechargePackageDto>();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromForm] AdminRechargePackageDto model, CancellationToken ct)
    {
        if (model.Amount <= 0)
        {
            TempData["Error"] = "Package amount must be greater than zero.";
            return RedirectToAction(nameof(Index));
        }

        var bonusAmount = model.BonusAmount;
        if (model.BonusPercentage > 0 && bonusAmount == 0)
        {
            bonusAmount = Math.Round(model.Amount * (model.BonusPercentage / 100m), 2);
        }

        var package = model with { BonusAmount = bonusAmount };
        var res = await _adminService.SaveRechargePackageAsync(package, ct);

        if (res.IsSuccess)
        {
            TempData["Success"] = "Recharge package saved successfully.";
        }
        else
        {
            TempData["Error"] = res.Error ?? "Failed to save package.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(long id, bool isActive, CancellationToken ct)
    {
        var res = await _adminService.TogglePackageStatusAsync(id, isActive, ct);
        if (res.IsSuccess)
        {
            TempData["Success"] = $"Package {(isActive ? "enabled" : "disabled")} successfully.";
        }
        else
        {
            TempData["Error"] = res.Error ?? "Failed to change status.";
        }

        return RedirectToAction(nameof(Index));
    }
}
