namespace Econyx.Worker.Services;

using Econyx.Domain.Repositories;

public sealed class LogCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LogCleanupService> _logger;

    public LogCleanupService(IServiceScopeFactory scopeFactory, ILogger<LogCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IAiRequestLogRepository>();

                var cutoff = DateTime.UtcNow.AddDays(-30);
                await repo.DeleteOlderThanAsync(cutoff, stoppingToken);

                _logger.LogInformation("AI request logs older than 30 days cleaned up");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during log cleanup");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
