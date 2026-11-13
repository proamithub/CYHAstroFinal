using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Infrastructure.Cache;
using CYHAstro.Infrastructure.Data;
using CYHAstro.Infrastructure.Identity;
using CYHAstro.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CYHAstro.Infrastructure;

/// <summary>
/// Infrastructure layer DI registration.
/// Called from each host application's Program.cs.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- Database (Dapper) ---
        services.AddSingleton<DbConnectionFactory>();
        services.AddSingleton<IDbConnectionFactory>(sp => sp.GetRequiredService<DbConnectionFactory>());

        // --- Identity DbContext (EF Core — only for ASP.NET Identity) ---
        var connStr = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

        services.AddDbContext<CYHAstroIdentityDbContext>(options =>
            options.UseSqlServer(connStr, sql =>
            {
                sql.CommandTimeout(30);
                sql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        // --- Repositories (Dapper) ---
        services.AddScoped<IWalletRepository, CYHAstro.Infrastructure.Data.Repositories.WalletRepository>();
        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IAstrologerRepository, CYHAstro.Infrastructure.Data.Repositories.AstrologerRepository>();
        services.AddScoped<IBirthProfileRepository, CYHAstro.Infrastructure.Data.Repositories.BirthProfileRepository>();

        // --- Redis Distributed Cache ---
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "cyhastro:";
            });
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            // Fallback to in-memory cache for local dev without Redis
            services.AddDistributedMemoryCache();
            services.AddSingleton<ICacheService, RedisCacheService>();
        }

        // --- Authentication & Security Services ---
        services.AddScoped<CYHAstro.Core.Interfaces.Services.ITokenService, CYHAstro.Infrastructure.Identity.TokenService>();
        services.AddScoped<CYHAstro.Core.Interfaces.Services.IAuthService, CYHAstro.Infrastructure.Identity.AuthService>();

        // --- Gemini AI Astrology Service ---
        services.Configure<CYHAstro.Core.Configuration.GeminiSettings>(
            configuration.GetSection(CYHAstro.Core.Configuration.GeminiSettings.SectionName));
        services.AddHttpClient<CYHAstro.Core.Interfaces.Services.IAiAstrologyService, CYHAstro.Infrastructure.Astrology.GeminiAiAstrologyService>();

        // --- LiveKit WebRTC Token Service ---
        services.Configure<CYHAstro.Core.Configuration.LiveKitSettings>(
            configuration.GetSection(CYHAstro.Core.Configuration.LiveKitSettings.SectionName));
        services.AddSingleton<CYHAstro.Core.Interfaces.Services.ILiveKitTokenService, CYHAstro.Infrastructure.LiveKit.LiveKitTokenService>();

        // --- Core Application Services ---
        services.AddScoped<CYHAstro.Core.Interfaces.Services.IProfileService, CYHAstro.Infrastructure.Services.ProfileService>();
        services.AddScoped<CYHAstro.Core.Interfaces.Services.IAstrologerService, CYHAstro.Infrastructure.Services.AstrologerService>();
        services.AddScoped<CYHAstro.Core.Interfaces.Services.IWalletService, CYHAstro.Infrastructure.Services.WalletService>();
        services.AddScoped<CYHAstro.Core.Interfaces.Services.IAstrologyCalculationService, CYHAstro.Infrastructure.Services.AstrologyCalculationService>();
        services.AddScoped<CYHAstro.Core.Interfaces.Services.IAdminService, CYHAstro.Infrastructure.Services.AdminService>();
        services.AddScoped<CYHAstro.Core.Interfaces.Services.IConsultationService, CYHAstro.Infrastructure.Services.ConsultationService>();
        services.AddScoped<CYHAstro.Core.Interfaces.Services.IPanchangService, CYHAstro.Infrastructure.Services.PanchangService>();
        services.AddScoped<CYHAstro.Core.Interfaces.Services.IKundliPdfService, CYHAstro.Infrastructure.Reports.KundliPdfGenerator>();
        services.AddScoped<CYHAstro.Core.Interfaces.Services.IMatchmakingPdfService, CYHAstro.Infrastructure.Reports.MatchmakingPdfGenerator>();
        services.AddScoped<CYHAstro.Core.Interfaces.Services.IAiChatService, CYHAstro.Infrastructure.Services.AiChatService>();
        services.AddScoped<CYHAstro.Core.Interfaces.Services.INotificationService, CYHAstro.Infrastructure.Services.NotificationService>();

        // --- Background Billing Engine ---
        services.AddHostedService<CYHAstro.Infrastructure.BackgroundJobs.ConsultationBillingBackgroundService>();

        return services;
    }
}
