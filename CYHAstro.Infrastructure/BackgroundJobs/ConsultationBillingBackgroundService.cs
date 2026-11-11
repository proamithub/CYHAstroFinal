using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CYHAstro.Infrastructure.BackgroundJobs;

public sealed class ConsultationBillingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsultationBillingBackgroundService> _logger;

    public ConsultationBillingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ConsultationBillingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Consultation Billing Background Service is initialized and active.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessActiveSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing consultation active billing tick");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessActiveSessionsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        var walletService = scope.ServiceProvider.GetRequiredService<IWalletService>();
        var consultationService = scope.ServiceProvider.GetRequiredService<IConsultationService>();

        using var conn = await db.CreateOpenConnectionAsync(ct);

        const string sqlActive = @"
            SELECT Id, PublicId, UserId, AstrologerId, RatePerMinute, ConnectedAt
            FROM dbo.ConsultationSessions
            WHERE Status = 'ACTIVE' AND IsDeleted = 0;
        ";

        var activeSessions = (await conn.QueryAsync<dynamic>(sqlActive)).ToList();
        if (!activeSessions.Any()) return;

        foreach (var sess in activeSessions)
        {
            long userId = (long)sess.UserId;
            string publicId = (string)sess.PublicId;
            decimal ratePerMin = (decimal)sess.RatePerMinute;

            var balRes = await walletService.GetBalanceAsync(userId, ct);
            if (balRes.IsSuccess)
            {
                var currentBalance = balRes.Value!.TotalBalance;

                // If user's wallet is completely depleted, auto-terminate session gracefully
                if (currentBalance <= 0)
                {
                    _logger.LogWarning("Auto-terminating active session {PublicId} due to zero wallet balance", publicId);
                    await consultationService.EndConsultationAsync(0, publicId, "INSUFFICIENT_FUNDS", ct);
                }
            }
        }
    }
}
