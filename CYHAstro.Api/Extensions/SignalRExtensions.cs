namespace CYHAstro.Api.Extensions;

public static class SignalRExtensions
{
    public static IServiceCollection AddCYHAstroSignalR(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConn = configuration.GetConnectionString("Redis");

        var signalR = services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = false; // Only true in Development
            options.MaximumReceiveMessageSize = 32 * 1024; // 32 KB max message
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        });

        if (!string.IsNullOrEmpty(redisConn))
        {
            // Redis backplane for multi-server SignalR
            signalR.AddStackExchangeRedis(redisConn, options =>
            {
                options.Configuration.ChannelPrefix =
                    StackExchange.Redis.RedisChannel.Literal("cyhastro:signalr");
            });
        }

        return services;
    }
}
