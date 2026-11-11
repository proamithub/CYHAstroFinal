using CynAstro.Api.Extensions;
using CynAstro.Api.Middleware;
using CynAstro.Infrastructure;
using CynAstro.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Serilog;

// ─────────────────────────────────────────────
// Bootstrap Serilog before the host starts
// ─────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ──────────────────────────────
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/cynastro-api-.log", rollingInterval: RollingInterval.Day));

    // ── Services ─────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // OpenAPI (built-in .NET 10 — no Swashbuckle conflict)
    builder.Services.AddOpenApi();

    // Infrastructure (Dapper + Redis + Identity EF + Repositories)
    builder.Services.AddInfrastructure(builder.Configuration);

    // ASP.NET Identity
    builder.Services.AddIdentity<CynAstroIdentityUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<CynAstroIdentityDbContext>()
    .AddDefaultTokenProviders();

    // JWT + Google Auth
    builder.Services.AddCynAstroAuthentication(builder.Configuration);

    // Authorization Policies
    builder.Services.AddCynAstroAuthorization();

    // SignalR + Redis Backplane
    builder.Services.AddCynAstroSignalR(builder.Configuration);

    // CORS
    builder.Services.AddCynAstroCors(builder.Configuration);

    // Rate Limiting
    builder.Services.AddCynAstroRateLimiting();

    // Response Compression
    builder.Services.AddResponseCompression(opts => opts.EnableForHttps = true);

    // Output Caching
    builder.Services.AddOutputCache(opts =>
    {
        opts.AddBasePolicy(b => b.NoCache());
    });

    // ── Build App ────────────────────────────
    var app = builder.Build();

    // ── Middleware Pipeline ───────────────────
    app.UseResponseCompression();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    // Security headers
    app.UseSecurityHeaders();

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();

    app.UseCors("CynAstroCors");
    app.UseRateLimiter();
    app.UseOutputCache();

    app.UseAuthentication();
    app.UseAuthorization();

    // Global error handler
    app.UseGlobalExceptionHandler();

    app.MapControllers();

    // SignalR Hubs (registered in Phase 15)
    // app.MapHub<ChatHub>("/hubs/chat");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CynAstro API failed to start.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program discoverable for integration tests
public partial class Program { }
