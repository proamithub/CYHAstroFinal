using CYHAstro.Core.DTOs.Admin;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
public sealed class PayoutsController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<PayoutsController> _logger;

    public PayoutsController(IAdminService adminService, ILogger<PayoutsController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status, CancellationToken ct)
    {
        ViewBag.Status = status;
        var res = await _adminService.GetAllSettlementsAsync(status, ct);
        var list = res.IsSuccess ? res.Value : new List<SettlementDto>();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(long id, string status, string? payoutReference, string? remarks, CancellationToken ct)
    {
        var res = await _adminService.ProcessSettlementAsync(id, status, payoutReference, remarks, ct);
        if (res.IsSuccess)
        {
            TempData["Success"] = $"Payout settlement #{id} marked as {status}.";
        }
        else
        {
            TempData["Error"] = res.Error ?? "Failed to process settlement.";
        }

        return RedirectToAction(nameof(Index));
    }
}
