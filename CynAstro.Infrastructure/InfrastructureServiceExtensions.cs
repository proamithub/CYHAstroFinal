using CynAstro.Core.Interfaces.Repositories;
using CynAstro.Infrastructure.Cache;
using CynAstro.Infrastructure.Data;
using CynAstro.Infrastructure.Identity;
using CynAstro.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CynAstro.Infrastructure;

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
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        // --- Identity DbContext (EF Core — only for ASP.NET Identity) ---
        var connStr = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("CynAstroDB")
            ?? throw new InvalidOperationException("Connection string ('DefaultConnection' or 'CynAstroDB') is required.");

        services.AddDbContext<CynAstroIdentityDbContext>(options =>
            options.UseSqlServer(connStr, sql =>
            {
                sql.CommandTimeout(30);
                sql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        // --- Repositories (Dapper) ---
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IAstrologerRepository, AstrologerRepository>();
        services.AddScoped<IBirthProfileRepository, BirthProfileRepository>();

        // --- Redis Distributed Cache ---
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "cynastro:";
            });
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            // Fallback to in-memory cache for local dev without Redis
            services.AddDistributedMemoryCache();
            services.AddSingleton<ICacheService, RedisCacheService>();
        }

        // --- Gemini AI Astrology Service ---
        services.Configure<CynAstro.Core.Configuration.GeminiSettings>(
            configuration.GetSection(CynAstro.Core.Configuration.GeminiSettings.SectionName));
        services.AddHttpClient<CynAstro.Core.Interfaces.Services.IAiAstrologyService, CynAstro.Infrastructure.Astrology.GeminiAiAstrologyService>();

        return services;
    }
}
