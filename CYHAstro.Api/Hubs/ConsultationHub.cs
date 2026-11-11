using System.Security.Claims;
using CYHAstro.Core.DTOs.Consultation;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CYHAstro.Api.Hubs;

[Authorize]
public sealed class ConsultationHub : Hub
{
    private readonly IConsultationService _consultationService;
    private readonly ILogger<ConsultationHub> _logger;

    public ConsultationHub(IConsultationService consultationService, ILogger<ConsultationHub> logger)
    {
        _consultationService = consultationService;
        _logger = logger;
    }

    public async Task JoinSessionGroup(string sessionPublicId)
    {
        if (string.IsNullOrWhiteSpace(sessionPublicId)) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionPublicId);

        var userId = GetCurrentUserId();
        var displayName = Context.User?.Identity?.Name ?? "User";

        _logger.LogInformation("User {UserId} joined consultation group {SessionPublicId}", userId, sessionPublicId);

        await Clients.OthersInGroup(sessionPublicId).SendAsync("UserJoinedSession", new
        {
            SessionPublicId = sessionPublicId,
            UserId = userId,
            DisplayName = displayName
        });
    }

    public async Task LeaveSessionGroup(string sessionPublicId)
    {
        if (string.IsNullOrWhiteSpace(sessionPublicId)) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionPublicId);

        var userId = GetCurrentUserId();
        await Clients.OthersInGroup(sessionPublicId).SendAsync("UserLeftSession", new
        {
            SessionPublicId = sessionPublicId,
            UserId = userId
        });
    }

    public async Task SendMessage(string sessionPublicId, string message)
    {
        if (string.IsNullOrWhiteSpace(sessionPublicId) || string.IsNullOrWhiteSpace(message)) return;

        var userId = GetCurrentUserId();
        var saveRes = await _consultationService.SaveMessageAsync(userId, sessionPublicId, message);

        if (saveRes.IsSuccess)
        {
            await Clients.Group(sessionPublicId).SendAsync("ReceiveMessage", saveRes.Value);
        }
        else
        {
            await Clients.Caller.SendAsync("MessageError", saveRes.Error ?? "Could not deliver message.");
        }
    }

    public async Task SendTyping(string sessionPublicId, bool isTyping)
    {
        if (string.IsNullOrWhiteSpace(sessionPublicId)) return;

        var userId = GetCurrentUserId();
        var displayName = Context.User?.Identity?.Name ?? "User";

        await Clients.OthersInGroup(sessionPublicId).SendAsync("UserTyping", new
        {
            SessionPublicId = sessionPublicId,
            UserId = userId,
            DisplayName = displayName,
            IsTyping = isTyping
        });
    }

    public async Task SignalMediaOffer(string sessionPublicId, object offer)
    {
        if (string.IsNullOrWhiteSpace(sessionPublicId)) return;
        await Clients.OthersInGroup(sessionPublicId).SendAsync("ReceiveMediaOffer", offer);
    }

    public async Task SignalMediaAnswer(string sessionPublicId, object answer)
    {
        if (string.IsNullOrWhiteSpace(sessionPublicId)) return;
        await Clients.OthersInGroup(sessionPublicId).SendAsync("ReceiveMediaAnswer", answer);
    }

    public async Task SignalIceCandidate(string sessionPublicId, object candidate)
    {
        if (string.IsNullOrWhiteSpace(sessionPublicId)) return;
        await Clients.OthersInGroup(sessionPublicId).SendAsync("ReceiveIceCandidate", candidate);
    }

    public async Task EndSession(string sessionPublicId, string? reason)
    {
        if (string.IsNullOrWhiteSpace(sessionPublicId)) return;

        var userId = GetCurrentUserId();
        var endRes = await _consultationService.EndConsultationAsync(userId, sessionPublicId, reason ?? "USER_END");

        if (endRes.IsSuccess)
        {
            await Clients.Group(sessionPublicId).SendAsync("SessionEnded", endRes.Value);
        }
    }

    private long GetCurrentUserId()
    {
        var val = Context.User?.FindFirstValue("userId") ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(val, out var id) ? id : 0;
    }
}
