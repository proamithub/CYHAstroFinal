using System.Security.Claims;
using CYHAstro.Core.DTOs.Consultation;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CYHAstro.Web.Areas.Astrologer.Controllers;

[Area("Astrologer")]
[Authorize(AuthenticationSchemes = "AstrologerCookie", Roles = "Astrologer")]
public sealed class ConsultationController : Controller
{
    private readonly IConsultationService _consultationService;
    private readonly IBirthProfileRepository _birthProfileRepo;
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<ConsultationController> _logger;
    private readonly IHubContext<CYHAstro.Web.Hubs.ConsultationHub> _hubContext;

    public ConsultationController(
        IConsultationService consultationService,
        IBirthProfileRepository birthProfileRepo,
        IDbConnectionFactory db,
        ILogger<ConsultationController> logger,
        IHubContext<CYHAstro.Web.Hubs.ConsultationHub> hubContext)
    {
        _consultationService = consultationService;
        _birthProfileRepo = birthProfileRepo;
        _db = db;
        _logger = logger;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> Accept(string id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth", new { area = "Astrologer" });

        var acceptRes = await _consultationService.AcceptConsultationAsync(userId, id, ct);
        if (acceptRes.IsSuccess)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            await _hubContext.Clients.Group(id).SendAsync("SessionAccepted", new {
                sessionPublicId = id,
                connectedAtUnix = now,
                message = "Astrologer has accepted the consultation. Session is now live!"
            });

            return RedirectToAction(nameof(Room), new { id });
        }

        TempData["Error"] = acceptRes.Error ?? "Could not accept consultation.";
        return RedirectToAction("Index", "Home", new { area = "Astrologer" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject([FromForm] string sessionPublicId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth", new { area = "Astrologer" });

        var res = await _consultationService.RejectConsultationAsync(userId, sessionPublicId, ct);
        if (res.IsSuccess)
        {
            await _hubContext.Clients.Group(sessionPublicId).SendAsync("SessionRejected", new {
                sessionPublicId = sessionPublicId,
                message = "Astrologer is currently unavailable."
            });
            TempData["Success"] = "Consultation request rejected.";
        }
        else
        {
            TempData["Error"] = res.Error ?? "Could not reject request.";
        }

        return RedirectToAction("Index", "Home", new { area = "Astrologer" });
    }

    [HttpGet]
    public async Task<IActionResult> Room(string id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth", new { area = "Astrologer" });

        var sessionRes = await _consultationService.GetConsultationByPublicIdAsync(id, ct);
        if (!sessionRes.IsSuccess || sessionRes.Value == null)
        {
            TempData["Error"] = "Consultation session not found.";
            return RedirectToAction("Index", "Home", new { area = "Astrologer" });
        }

        var session = sessionRes.Value;

        // Verify that this session belongs to the logged-in astrologer
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var astro = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT Id, UserId FROM dbo.Astrologers WHERE UserId = @UserId", new { UserId = userId });
        if (astro == null || (long?)astro.Id != session.AstrologerId)
        {
            TempData["Error"] = "You are not authorized for this consultation session.";
            return RedirectToAction("Index", "Home", new { area = "Astrologer" });
        }

        // LiveKit token for Astrologer
        var tokenRes = await _consultationService.GetLiveKitTokenAsync(userId, id, ct);
        ViewBag.LiveKitToken = tokenRes.IsSuccess ? tokenRes.Value : string.Empty;

        // Fetch seeker's primary birth profile for instant Kundli view
        var profiles = await _birthProfileRepo.GetByUserIdAsync(session.UserId, ct);
        var primaryProfile = profiles.FirstOrDefault(p => p.IsPrimary) ?? profiles.FirstOrDefault();
        ViewBag.SeekerProfile = primaryProfile;

        return View(session);
    }

    [HttpGet]
    public IActionResult EndSession()
    {
        return RedirectToAction("Index", "Home", new { area = "Astrologer" });
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

            return RedirectToAction("Login", "Auth", new { area = "Astrologer" });
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
                return BadRequest(new { success = false, message = "Session ID is required." });

            return RedirectToAction("Index", "Home", new { area = "Astrologer" });
        }

        var res = await _consultationService.EndConsultationAsync(userId, targetId, "ASTROLOGER_END", ct);
        if (res.IsSuccess)
        {
            var val = res.Value!;
            await _hubContext.Clients.Group(targetId).SendAsync("SessionEnded", new {
                sessionPublicId = targetId,
                durationSeconds = val.DurationSeconds,
                totalCharge = val.TotalCharge,
                astrologerEarnings = val.AstrologerEarnings,
                endedBy = "ASTROLOGER"
            });

            if (Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                return Ok(new {
                    success = true,
                    durationSeconds = val.DurationSeconds,
                    totalCharge = val.TotalCharge,
                    astrologerEarnings = val.AstrologerEarnings
                });
            }

            TempData["Success"] = $"Consultation ended successfully. Duration: {val.DurationSeconds / 60}m {val.DurationSeconds % 60}s. Your Net Earnings: ₹{val.AstrologerEarnings:F2} (Platform Commission: ₹{val.PlatformCommission:F2}).";
        }
        else
        {
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
                return BadRequest(new { success = false, error = res.Error });

            TempData["Error"] = res.Error ?? "Could not end session.";
        }

        return RedirectToAction("Index", "Home", new { area = "Astrologer" });
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}
