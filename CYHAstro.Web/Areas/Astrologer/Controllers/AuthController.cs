using System.Security.Claims;
using CYHAstro.Infrastructure.Data;
using CYHAstro.Infrastructure.Identity;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Astrologer.Controllers;

[Area("Astrologer")]
[AllowAnonymous]
public sealed class AuthController : Controller
{
    private readonly SignInManager<CYHAstroIdentityUser> _signInManager;
    private readonly UserManager<CYHAstroIdentityUser> _userManager;
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<CYHAstroIdentityUser> signInManager,
        UserManager<CYHAstroIdentityUser> userManager,
        IDbConnectionFactory db,
        ILogger<AuthController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Astrologer"))
        {
            return RedirectToAction("Index", "Home", new { area = "Astrologer" });
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Email and password are required.");
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ModelState.AddModelError("", "Invalid astrologer credentials.");
            return View();
        }

        if (!await _userManager.IsInRoleAsync(user, "Astrologer"))
        {
            ModelState.AddModelError("", "Access denied. Only registered and approved astrologers can log in here.");
            return View();
        }

        var checkPassword = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!checkPassword.Succeeded)
        {
            ModelState.AddModelError("", "Invalid astrologer credentials.");
            return View();
        }

        // Fetch UserProfiles.Id
        using var conn = await _db.CreateOpenConnectionAsync();
        var profile = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT Id, DisplayName, PublicId FROM dbo.UserProfiles WHERE Email = @Email", new { Email = email });
        long userProfileId = profile != null ? (long)profile.Id : 0;

        // Fetch Astrologer Id
        var astro = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT Id, PublicId FROM dbo.Astrologers WHERE UserId = @UserId", new { UserId = userProfileId });
        long astroId = astro != null ? (long)astro.Id : 0;
        string astroPublicId = astro != null ? (string)astro.PublicId : "";

        // Build Claims Principal for AstrologerCookie
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.DisplayName ?? user.UserName ?? "Astrologer"),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Role, "Astrologer"),
            new("userId", userProfileId.ToString()),
            new("astrologerId", astroId.ToString()),
            new("astrologerPublicId", astroPublicId)
        };

        var identity = new ClaimsIdentity(claims, "AstrologerCookie");
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(12)
        };

        await HttpContext.SignInAsync("AstrologerCookie", principal, authProperties);
        _logger.LogInformation("Astrologer {Email} logged into Astrologer Portal.", email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home", new { area = "Astrologer" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("AstrologerCookie");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
