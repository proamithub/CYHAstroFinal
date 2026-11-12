using System.Security.Claims;
using CYHAstro.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Admin.Controllers;

[Area("Admin")]
public sealed class AuthController : Controller
{
    private readonly SignInManager<CYHAstroIdentityUser> _signInManager;
    private readonly UserManager<CYHAstroIdentityUser> _userManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<CYHAstroIdentityUser> signInManager,
        UserManager<CYHAstroIdentityUser> userManager,
        ILogger<AuthController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Please enter both email and password.";
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ViewBag.Error = "Invalid administrator credentials.";
            return View();
        }

        var check = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!check.Succeeded)
        {
            if (check.IsLockedOut)
            {
                ViewBag.Error = "Account is temporarily locked due to multiple failed attempts. Please retry later.";
            }
            else
            {
                ViewBag.Error = "Invalid administrator credentials.";
            }
            return View();
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (!isAdmin)
        {
            ViewBag.Error = "Unauthorized: Access restricted strictly to CYHAstro system administrators.";
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email ?? "Administrator"),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Role, "Admin")
        };

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var r in roles)
        {
            if (r != "Admin") claims.Add(new Claim(ClaimTypes.Role, r));
        }

        var identity = new ClaimsIdentity(claims, "AdminCookie");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("AdminCookie", principal, new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(12)
        });

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home", new { area = "Admin" });
    }

    [HttpGet]
    [HttpPost]
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("AdminCookie");
        return RedirectToAction("Login", "Auth", new { area = "Admin" });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
