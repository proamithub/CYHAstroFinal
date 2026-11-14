using CYHAstro.Core.Interfaces.Services;

namespace CYHAstro.Web.Workers;

public sealed class StaleSessionWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<StaleSessionWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    public StaleSessionWorker(IServiceProvider services, ILogger<StaleSessionWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StaleSessionWorker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
                using var scope = _services.CreateScope();
                var consultationService = scope.ServiceProvider.GetRequiredService<IConsultationService>();
                var result = await consultationService.AutoEndStaleSessionsAsync(stoppingToken);
                if (result.IsSuccess && result.Value)
                {
                    _logger.LogInformation("StaleSessionWorker: auto-ended stale sessions.");
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StaleSessionWorker encountered an error.");
            }
        }
    }
}
