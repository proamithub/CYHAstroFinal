using System.Security.Claims;
using CYHAstro.Core.DTOs.Astrologer;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CYHAstro.Web.Areas.Astrologer.Controllers;

[Area("Astrologer")]
[Authorize(AuthenticationSchemes = "AstrologerCookie", Roles = "Astrologer")]
public sealed class HomeController : Controller
{
    private readonly IAstrologerService _astrologerService;
    private readonly IConsultationService _consultationService;
    private readonly IHubContext<ConsultationHub> _hubContext;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IAstrologerService astrologerService,
        IConsultationService consultationService,
        IHubContext<ConsultationHub> hubContext,
        ILogger<HomeController> logger)
    {
        _astrologerService = astrologerService;
        _consultationService = consultationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth", new { area = "Astrologer" });

        var dashRes = await _astrologerService.GetDashboardAsync(userId, ct);
        if (!dashRes.IsSuccess)
        {
            TempData["Error"] = dashRes.Error ?? "Could not load astrologer profile.";
            return View(new AstrologerDashboardDto());
        }

        return View(dashRes.Value);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleAvailability([FromBody] ToggleAvailabilityRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var astroRes = await _astrologerService.GetAstrologerByUserIdAsync(userId, ct);
        if (!astroRes.IsSuccess || astroRes.Value == null)
        {
            return NotFound(new { error = "Astrologer record not found." });
        }

        var updateRes = await _astrologerService.SetAvailabilityAsync(astroRes.Value.Id, request.IsAvailable, ct);
        if (updateRes.IsSuccess)
        {
            _logger.LogInformation("Astrologer {AstroId} ({PublicId}) toggled availability to {IsAvailable}",
                astroRes.Value.Id, astroRes.Value.PublicId, request.IsAvailable);

            // Broadcast real-time presence change to all seekers/clients instantly
            await _hubContext.Clients.All.SendAsync("AstrologerStatusChanged", new
            {
                astrologerId = astroRes.Value.Id,
                publicId = astroRes.Value.PublicId,
                isAvailable = request.IsAvailable
            }, ct);

            return Ok(new { isAvailable = request.IsAvailable, message = "Status updated." });
        }

        return BadRequest(new { error = updateRes.Error });
    }

    [HttpGet]
    public async Task<IActionResult> GetPendingRequests(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var res = await _consultationService.GetIncomingRequestsForAstrologerAsync(userId, ct);
        if (res.IsSuccess)
        {
            return Ok(res.Value);
        }

        return BadRequest(new { error = res.Error });
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}

public sealed class ToggleAvailabilityRequest
{
    public bool IsAvailable { get; set; }
}
