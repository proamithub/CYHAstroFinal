using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure;
using CYHAstro.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CYHAstro.Tests.Unit;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ShouldResolveAllServicesAndRepositories_WithoutValidationErrors()
    {
        // Arrange
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=TestDb;Integrated Security=true;TrustServerCertificate=true;",
            ["ConnectionStrings:Redis"] = "",
            ["LiveKit:ApiKey"] = "devkey",
            ["LiveKit:ApiSecret"] = "12345678901234567890123456789012",
            ["LiveKit:ServerUrl"] = "wss://test.livekit.cloud",
            ["Gemini:ApiKey"] = "test-gemini-key",
            ["Gemini:Model"] = "gemini-2.5-flash",
            ["Jwt:Secret"] = "super-secret-key-that-is-at-least-32-characters-long!"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);

        // Add ASP.NET Identity as configured in each host Program.cs
        services.AddIdentity<CYHAstro.Infrastructure.Identity.CYHAstroIdentityUser, Microsoft.AspNetCore.Identity.IdentityRole>()
            .AddEntityFrameworkStores<CYHAstro.Infrastructure.Identity.CYHAstroIdentityDbContext>();

        // Act - Validate container on build exactly like ASP.NET Core Development mode does
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        // Assert - Verify resolution of IDbConnectionFactory and DbConnectionFactory
        var dbFactory = provider.GetRequiredService<IDbConnectionFactory>();
        Assert.NotNull(dbFactory);

        var concreteDb = provider.GetRequiredService<DbConnectionFactory>();
        Assert.NotNull(concreteDb);

        // Assert - Verify resolution of Scoped Repositories and Services within scope
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.NotNull(sp.GetRequiredService<IAstrologerRepository>());
        Assert.NotNull(sp.GetRequiredService<IBirthProfileRepository>());
        Assert.NotNull(sp.GetRequiredService<IWalletRepository>());
        Assert.NotNull(sp.GetRequiredService<IConsultationRepository>());

        Assert.NotNull(sp.GetRequiredService<IProfileService>());
        Assert.NotNull(sp.GetRequiredService<IAstrologerService>());
        Assert.NotNull(sp.GetRequiredService<IWalletService>());
        Assert.NotNull(sp.GetRequiredService<IAstrologyCalculationService>());
        Assert.NotNull(sp.GetRequiredService<IAdminService>());
        Assert.NotNull(sp.GetRequiredService<IConsultationService>());
    }
}
