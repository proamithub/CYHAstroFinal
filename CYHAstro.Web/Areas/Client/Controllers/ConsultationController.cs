using System.Security.Claims;
using CYHAstro.Core.DTOs.Consultation;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Client.Controllers;

[Area("Client")]
[Authorize(AuthenticationSchemes = "ClientCookie")]
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
        if (userId <= 0) return RedirectToAction("Login", "Auth", new { area = "Client" });

        var sessionRes = await _consultationService.GetConsultationByPublicIdAsync(id, ct);
        if (!sessionRes.IsSuccess)
        {
            TempData["Error"] = "Consultation session not found.";
            return RedirectToAction("Index", "Astrologer", new { area = "Client" });
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
        if (userId <= 0) return RedirectToAction("Login", "Auth", new { area = "Client" });

        var res = await _consultationService.RequestConsultationAsync(
            userId, new RequestConsultationDto { AstrologerPublicId = astroPublicId, SessionType = sessionType }, ct);

        if (res.IsSuccess)
        {
            return RedirectToAction(nameof(Room), new { id = res.Value!.PublicId });
        }

        TempData["Error"] = res.Error ?? "Could not initiate consultation.";
        return RedirectToAction("Detail", "Astrologer", new { id = astroPublicId, area = "Client" });
    }

    [HttpGet]
    public IActionResult EndSession()
    {
        return RedirectToAction("Index", "Astrologer", new { area = "Client" });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> EndSession([FromForm] string? sessionPublicId, [FromQuery] string? id, CancellationToken ct)
    {
        var targetId = !string.IsNullOrWhiteSpace(sessionPublicId) ? sessionPublicId : id;
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
                return Unauthorized(new { success = false, message = "Please sign in." });

            return RedirectToAction("Login", "Auth", new { area = "Client" });
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
                return BadRequest(new { success = false, message = "Session ID is required." });

            return RedirectToAction("Index", "Astrologer", new { area = "Client" });
        }

        var res = await _consultationService.EndConsultationAsync(userId, targetId, "USER_END", ct);
        if (res.IsSuccess)
        {
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                return Ok(new
                {
                    success = true,
                    durationSeconds = res.Value!.DurationSeconds,
                    totalCharge = res.Value.TotalCharge,
                    remainingBalance = res.Value.UserRemainingBalance
                });
            }

            TempData["Success"] = $"Consultation ended. Total Duration: {res.Value!.DurationSeconds / 60}m {res.Value.DurationSeconds % 60}s. Billed: ₹{res.Value.TotalCharge:F2}. Remaining Wallet Balance: ₹{res.Value.UserRemainingBalance:F2}";
        }
        else
        {
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
                return BadRequest(new { success = false, error = res.Error });

            TempData["Error"] = res.Error ?? "Could not finalize session.";
        }

        return RedirectToAction("Index", "Astrologer", new { area = "Client" });
    }

    [HttpPost("Consultation/SubmitReview")]
    public async Task<IActionResult> SubmitReview([FromBody] CYHAstro.Core.DTOs.Astrologer.SubmitReviewRequestDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var res = await _consultationService.SubmitReviewAsync(userId, request, ct);
        if (res.IsSuccess)
        {
            return Ok(new { success = true, message = "Thank you! Your rating and review have been recorded." });
        }

        return BadRequest(new { success = false, error = res.Error });
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(val, out var id) ? id : 0;
    }
}
