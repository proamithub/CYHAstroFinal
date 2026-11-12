using CYHAstro.Infrastructure;
using CYHAstro.Infrastructure.Identity;
using CYHAstro.Web.Areas.Admin.Services;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Load Local Secrets if present
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// ── Infrastructure (Dapper + Identity DbContext + Core Services + Panchang + QuestPDF) ──
builder.Services.AddInfrastructure(builder.Configuration);

// ── ASP.NET Identity Core ──
builder.Services.AddIdentity<CYHAstroIdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<CYHAstroIdentityDbContext>()
.AddDefaultTokenProviders();

// ── Dual Isolated Authentication Schemes (Client & Admin) ──
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "ClientCookie";
    options.DefaultAuthenticateScheme = "ClientCookie";
    options.DefaultChallengeScheme = "ClientCookie";
    options.DefaultSignInScheme = "ClientCookie";
})
.AddCookie("ClientCookie", options =>
{
    options.Cookie.Name = "CYHAstro.Session";
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
})
.AddCookie("AdminCookie", options =>
{
    options.Cookie.Name = "CYHAstro.AdminSession";
    options.LoginPath = "/Admin/Auth/Login";
    options.LogoutPath = "/Admin/Auth/Logout";
    options.AccessDeniedPath = "/Admin/Auth/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ── Authorization Policies ──
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.AuthenticationSchemes.Add("AdminCookie");
        policy.RequireRole("Admin");
    });
    options.AddPolicy("ClientOnly", policy =>
    {
        policy.AuthenticationSchemes.Add("ClientCookie");
        policy.RequireAuthenticatedUser();
    });
});

// ── MVC Controllers with Views ──
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ── Seed Initial SuperAdmin Credentials ──
try
{
    await AdminDbSeeder.SeedAdminAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Admin seed skipped or encountered error during startup");
}

// ── HTTP Pipeline ──
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ── Area & Default Route Definitions ──
// 1. Admin Area: /Admin/...
app.MapAreaControllerRoute(
    name: "AdminArea",
    areaName: "Admin",
    pattern: "Admin/{controller=Home}/{action=Index}/{id?}");

// 2. Client Area Explicit: /Client/...
app.MapAreaControllerRoute(
    name: "ClientArea",
    areaName: "Client",
    pattern: "Client/{controller=Home}/{action=Index}/{id?}");

// 3. Default Root Route: / maps cleanly to Client Area
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "Client" });

app.Run();
