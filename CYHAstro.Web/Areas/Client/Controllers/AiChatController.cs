using System.Security.Claims;
using CYHAstro.Core.DTOs.Ai;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Client.Controllers;

[Area("Client")]
public sealed class AiChatController : Controller
{
    private readonly IAiChatService _aiChatService;
    private readonly IBirthProfileRepository _birthProfileRepo;
    private readonly ILogger<AiChatController> _logger;

    public AiChatController(
        IAiChatService aiChatService,
        IBirthProfileRepository birthProfileRepo,
        ILogger<AiChatController> logger)
    {
        _aiChatService = aiChatService;
        _birthProfileRepo = birthProfileRepo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var sessions = new List<AiChatSessionDto>();
        AiChatSessionDto? currentSession = null;
        var birthProfiles = new List<CYHAstro.Core.Entities.BirthProfile>();

        if (userId > 0)
        {
            var sessionsRes = await _aiChatService.GetUserSessionsAsync(userId, ct);
            if (sessionsRes.IsSuccess) sessions = sessionsRes.Value!;

            var profilesRes = await _birthProfileRepo.GetByUserIdAsync(userId, ct);
            birthProfiles = profilesRes.ToList();

            if (!string.IsNullOrWhiteSpace(id))
            {
                var sRes = await _aiChatService.GetSessionAsync(userId, id, ct);
                if (sRes.IsSuccess) currentSession = sRes.Value;
            }
            else if (sessions.Any())
            {
                var sRes = await _aiChatService.GetSessionAsync(userId, sessions.First().PublicId, ct);
                if (sRes.IsSuccess) currentSession = sRes.Value;
            }
        }

        ViewBag.Sessions = sessions;
        ViewBag.CurrentSession = currentSession;
        ViewBag.BirthProfiles = birthProfiles;
        ViewBag.IsGuest = userId <= 0;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendAiChatMessageRequestDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            // Allow demo message for unauthenticated guest
            return Json(new
            {
                success = true,
                isGuest = true,
                data = new
                {
                    sessionPublicId = "guest_demo",
                    title = "Guest Consultation",
                    reply = new
                    {
                        role = "assistant",
                        content = "Namaste! To save your AI astrological readings, link your personal Kundli, and get continuous astrological predictions, please Sign In to your CYHAstro account.",
                        timestamp = DateTime.UtcNow
                    }
                }
            });
        }

        var res = await _aiChatService.SendMessageAsync(userId, request, ct);
        if (res.IsSuccess)
        {
            return Json(new { success = true, data = res.Value });
        }

        return Json(new { success = false, error = res.Error });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSession(string id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var res = await _aiChatService.DeleteSessionAsync(userId, id, ct);
        return Json(new { success = res.IsSuccess });
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}
