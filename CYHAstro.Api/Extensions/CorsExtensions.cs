namespace CYHAstro.Api.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddCYHAstroCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5001", "http://localhost:5002"];

        services.AddCors(options =>
        {
            options.AddPolicy("CYHAstroCors", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials() // Required for SignalR
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });

        return services;
    }
}
