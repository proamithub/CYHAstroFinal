using System.Security.Claims;
using CYHAstro.Core.DTOs.Auth;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Client.Controllers;

[Area("Client")]
public sealed class AuthController : Controller
{
    private const string AuthScheme = "ClientCookie";

    private readonly SignInManager<CYHAstroIdentityUser> _signInManager;
    private readonly UserManager<CYHAstroIdentityUser> _userManager;
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<CYHAstroIdentityUser> signInManager,
        UserManager<CYHAstroIdentityUser> userManager,
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home", new { area = "Client" });
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequestDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var authResult = await _authService.LoginAsync(model, ip, userAgent);
        if (!authResult.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, authResult.Error ?? "Invalid login credentials.");
            return View(model);
        }

        var identityUser = await _userManager.FindByEmailAsync(model.Email);
        if (identityUser != null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, identityUser.Id),
                new("userId", authResult.Value!.User.Id.ToString()),
                new("publicId", authResult.Value.User.PublicId),
                new(ClaimTypes.Name, authResult.Value.User.DisplayName),
                new(ClaimTypes.Email, identityUser.Email!)
            };

            var roles = await _userManager.GetRolesAsync(identityUser);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, AuthScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(AuthScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        TempData["Success"] = $"Welcome back, {authResult.Value!.User.DisplayName}!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home", new { area = "Client" });
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home", new { area = "Client" });
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequestDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var regResult = await _authService.RegisterAsync(model, ip, userAgent);
        if (!regResult.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, regResult.Error ?? "Registration failed.");
            return View(model);
        }

        var identityUser = await _userManager.FindByEmailAsync(model.Email);
        if (identityUser != null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, identityUser.Id),
                new("userId", regResult.Value!.User.Id.ToString()),
                new("publicId", regResult.Value.User.PublicId),
                new(ClaimTypes.Name, regResult.Value.User.DisplayName),
                new(ClaimTypes.Email, identityUser.Email!)
            };

            var identity = new ClaimsIdentity(claims, AuthScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(AuthScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        TempData["Success"] = "Account created successfully! Welcome to CYHAstro.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard", new { area = "Client" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoogleLogin([FromForm] string idToken, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            TempData["Error"] = "Google sign-in failed. Please try again.";
            return RedirectToAction(nameof(Login));
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var request = new GoogleLoginRequestDto { IdToken = idToken };
        var authResult = await _authService.GoogleLoginAsync(request, ip, userAgent, CancellationToken.None);

        if (!authResult.IsSuccess)
        {
            TempData["Error"] = authResult.Error ?? "Google sign-in failed.";
            return RedirectToAction(nameof(Login));
        }

        var identityUser = await _userManager.FindByEmailAsync(authResult.Value!.User.Email);
        if (identityUser == null)
        {
            identityUser = new CYHAstroIdentityUser
            {
                UserName = authResult.Value.User.Email,
                Email = authResult.Value.User.Email,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(identityUser);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, identityUser.Id),
            new("userId", authResult.Value.User.Id.ToString()),
            new("publicId", authResult.Value.User.PublicId),
            new(ClaimTypes.Name, authResult.Value.User.DisplayName),
            new(ClaimTypes.Email, identityUser.Email!)
        };

        var roles = await _userManager.GetRolesAsync(identityUser);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, AuthScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(AuthScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        });

        TempData["Success"] = $"Welcome, {authResult.Value.User.DisplayName}!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home", new { area = "Client" });
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthScheme);
        await _signInManager.SignOutAsync();
        TempData["Success"] = "You have been signed out safely.";
        return RedirectToAction("Index", "Home", new { area = "Client" });
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
