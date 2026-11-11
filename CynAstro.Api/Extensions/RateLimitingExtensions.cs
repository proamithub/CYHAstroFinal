using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CynAstro.Api.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCynAstroRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global: 100 requests per minute per IP
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Strict: Auth endpoints — 10 per minute per IP
            options.AddFixedWindowLimiter("auth", opts =>
            {
                opts.PermitLimit = 10;
                opts.Window = TimeSpan.FromMinutes(1);
                opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opts.QueueLimit = 0;
            });

            // API: 300 per minute per authenticated user
            options.AddFixedWindowLimiter("api", opts =>
            {
                opts.PermitLimit = 300;
                opts.Window = TimeSpan.FromMinutes(1);
                opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opts.QueueLimit = 2;
            });
        });

        return services;
    }
}
