using System.Security.Claims;
using CYHAstro.Core.DTOs.Astrologer;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Astrologer.Controllers;

[Area("Astrologer")]
[Authorize(AuthenticationSchemes = "AstrologerCookie", Roles = "Astrologer")]
public sealed class EarningsController : Controller
{
    private readonly IAstrologerService _astrologerService;
    private readonly ILogger<EarningsController> _logger;

    public EarningsController(IAstrologerService astrologerService, ILogger<EarningsController> logger)
    {
        _astrologerService = astrologerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth", new { area = "Astrologer" });

        var earnRes = await _astrologerService.GetEarningsHistoryAsync(userId, ct);
        var earnings = earnRes.IsSuccess ? earnRes.Value! : new List<AstrologerEarningItemDto>();

        var dashRes = await _astrologerService.GetDashboardAsync(userId, ct);
        ViewBag.Dashboard = dashRes.IsSuccess ? dashRes.Value! : new AstrologerDashboardDto();

        return View(earnings);
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}
