using CYHAstro.Core.DTOs.Admin;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
public sealed class UsersController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IAdminService adminService, ILogger<UsersController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, CancellationToken ct)
    {
        ViewBag.Search = search;
        var res = await _adminService.GetUsersAsync(search, ct);
        var list = res.IsSuccess ? res.Value : new List<AdminUserDto>();
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(long id, bool isActive, CancellationToken ct)
    {
        var res = await _adminService.ToggleUserStatusAsync(id, isActive, ct);
        if (res.IsSuccess)
        {
            TempData["Success"] = $"User #{id} status changed to {(isActive ? "Active" : "Suspended")}.";
        }
        else
        {
            TempData["Error"] = res.Error ?? "Failed to update user status.";
        }

        return RedirectToAction(nameof(Index));
    }
}
