using System.Security.Claims;
using CYHAstro.Core.DTOs.Consultation;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Client.Controllers;

[Authorize]
public sealed class ConsultationController : Controller
{
    private readonly IConsultationService _consultationService;
    private readonly IWalletService _walletService;
    private readonly ILogger<ConsultationController> _logger;

    public ConsultationController(
        IConsultationService consultationService,
        IWalletService walletService,
        ILogger<ConsultationController> logger)
    {
        _consultationService = consultationService;
        _walletService = walletService;
        _logger = logger;
    }

    [HttpGet("Consultation/Room/{id}")]
    public async Task<IActionResult> Room(string id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth");

        var sessionRes = await _consultationService.GetConsultationByPublicIdAsync(id, ct);
        if (!sessionRes.IsSuccess)
        {
            TempData["Error"] = "Consultation session not found.";
            return RedirectToAction("Index", "Astrologer");
        }

        var balanceRes = await _walletService.GetBalanceAsync(userId, ct);
        ViewBag.WalletBalance = balanceRes.IsSuccess ? balanceRes.Value!.TotalBalance : 0m;

        var tokenRes = await _consultationService.GetLiveKitTokenAsync(userId, id, ct);
        ViewBag.LiveKitToken = tokenRes.IsSuccess ? tokenRes.Value : string.Empty;

        return View(sessionRes.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestSession([FromForm] string astroPublicId, [FromForm] string sessionType, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth");

        var res = await _consultationService.RequestConsultationAsync(
            userId, new RequestConsultationDto { AstrologerPublicId = astroPublicId, SessionType = sessionType }, ct);

        if (res.IsSuccess)
        {
            return RedirectToAction(nameof(Room), new { id = res.Value!.PublicId });
        }

        TempData["Error"] = res.Error ?? "Could not initiate consultation.";
        return RedirectToAction("Detail", "Astrologer", new { id = astroPublicId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndSession([FromForm] string sessionPublicId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth");

        var res = await _consultationService.EndConsultationAsync(userId, sessionPublicId, "USER_END", ct);
        if (res.IsSuccess)
        {
            TempData["Success"] = $"Consultation ended. Total Duration: {res.Value!.DurationSeconds / 60}m {res.Value.DurationSeconds % 60}s. Billed: ₹{res.Value.TotalCharge:F2}. Remaining Wallet Balance: ₹{res.Value.UserRemainingBalance:F2}";
        }
        else
        {
            TempData["Error"] = res.Error ?? "Could not finalize session.";
        }

        return RedirectToAction("Index", "Astrologer");
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(val, out var id) ? id : 0;
    }
}
