using System.Security.Claims;
using CYHAstro.Core.DTOs.Consultation;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Api.Controllers.v1;

[ApiController]
[Route("api/v1/consultations")]
[Authorize]
public sealed class ConsultationsController : ControllerBase
{
    private readonly IConsultationService _consultationService;
    private readonly ILogger<ConsultationsController> _logger;

    public ConsultationsController(IConsultationService consultationService, ILogger<ConsultationsController> logger)
    {
        _consultationService = consultationService;
        _logger = logger;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestConsultation([FromBody] RequestConsultationDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var res = await _consultationService.RequestConsultationAsync(userId, request, ct);
        return res.Match<IActionResult>(
            data => Ok(data),
            err => BadRequest(new { error = err }));
    }

    [HttpPost("{publicId}/accept")]
    public async Task<IActionResult> AcceptConsultation(string publicId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var res = await _consultationService.AcceptConsultationAsync(userId, publicId, ct);
        return res.Match<IActionResult>(
            data => Ok(data),
            err => BadRequest(new { error = err }));
    }

    [HttpPost("{publicId}/end")]
    public async Task<IActionResult> EndConsultation(string publicId, [FromBody] EndConsultationDto? body, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var res = await _consultationService.EndConsultationAsync(userId, publicId, body?.Reason ?? "USER_END", ct);
        return res.Match<IActionResult>(
            data => Ok(data),
            err => BadRequest(new { error = err }));
    }

    [HttpGet("{publicId}")]
    public async Task<IActionResult> GetConsultation(string publicId, CancellationToken ct)
    {
        var res = await _consultationService.GetConsultationByPublicIdAsync(publicId, ct);
        return res.Match<IActionResult>(
            data => Ok(data),
            err => NotFound(new { error = err }));
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var res = await _consultationService.GetActiveConsultationAsync(userId, ct);
        return res.Match<IActionResult>(
            data => Ok(data),
            err => BadRequest(new { error = err }));
    }

    [HttpGet("{publicId}/token")]
    public async Task<IActionResult> GetToken(string publicId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var res = await _consultationService.GetLiveKitTokenAsync(userId, publicId, ct);
        return res.Match<IActionResult>(
            token => Ok(new { token }),
            err => BadRequest(new { error = err }));
    }

    [HttpGet("{publicId}/messages")]
    public async Task<IActionResult> GetMessages(string publicId, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var res = await _consultationService.GetMessagesAsync(publicId, limit, ct);
        return res.Match<IActionResult>(
            msgs => Ok(msgs),
            err => BadRequest(new { error = err }));
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(val, out var id) ? id : 0;
    }
}
