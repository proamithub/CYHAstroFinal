using CYHAstro.Core.DTOs.Admin;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Admin.Controllers;

[Authorize(Roles = "Admin")]
public sealed class HomeController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IAdminService adminService, ILogger<HomeController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var statsRes = await _adminService.GetDashboardStatsAsync(ct);
        var stats = statsRes.IsSuccess ? statsRes.Value : new AdminDashboardDto();
        return View(stats);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Error()
    {
        return View();
    }
}
