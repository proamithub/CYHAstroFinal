using CYHAstro.Core.DTOs.Admin;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
public sealed class TransactionsController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(IAdminService adminService, ILogger<TransactionsController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? type, CancellationToken ct)
    {
        ViewBag.CurrentType = type;
        var res = await _adminService.GetPlatformTransactionsAsync(type, 100, ct);
        var list = res.IsSuccess ? res.Value : new List<AdminTransactionDto>();
        return View(list);
    }
}
