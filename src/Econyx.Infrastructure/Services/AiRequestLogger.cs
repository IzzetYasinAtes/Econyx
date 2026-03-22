namespace Econyx.Infrastructure.Services;

using Econyx.Application.Ports;
using Econyx.Domain.Entities;
using Econyx.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class AiRequestLogger : IAiRequestLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiRequestLogger> _logger;

    public AiRequestLogger(IServiceScopeFactory scopeFactory, ILogger<AiRequestLogger> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task LogAsync(
        string provider, string modelId, string marketQuestion,
        string prompt, string? response, string? parsedReasoning,
        decimal? fairValue, decimal? confidence,
        int inputTokens, int outputTokens, decimal costUsd,
        bool isSuccess, bool isCacheHit, string? errorMessage,
        CancellationToken ct = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<EconyxDbContext>();

            var log = AiRequestLog.Create(
                provider, modelId, marketQuestion, prompt, response,
                parsedReasoning, fairValue, confidence,
                inputTokens, outputTokens, costUsd,
                isSuccess, isCacheHit, errorMessage);

            await context.AiRequestLogs.AddAsync(log, ct);
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            LogPersistFailed(_logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to persist AI request log")]
    private static partial void LogPersistFailed(ILogger logger, Exception ex);
}
