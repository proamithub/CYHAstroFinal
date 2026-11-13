using CYHAstro.Core.DTOs.Admin;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
public sealed class ConsultationsController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<ConsultationsController> _logger;

    public ConsultationsController(IAdminService adminService, ILogger<ConsultationsController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status, CancellationToken ct)
    {
        ViewBag.Status = status;
        var res = await _adminService.GetAllConsultationsAsync(status, 100, ct);
        var list = res.IsSuccess ? res.Value : new List<AdminConsultationDto>();
        return View(list);
    }
}
