namespace Econyx.Application.Ports;

public interface IAiRequestLogger
{
    Task LogAsync(
        string provider,
        string modelId,
        string marketQuestion,
        string prompt,
        string? response,
        string? parsedReasoning,
        decimal? fairValue,
        decimal? confidence,
        int inputTokens,
        int outputTokens,
        decimal costUsd,
        bool isSuccess,
        bool isCacheHit,
        string? errorMessage,
        CancellationToken ct = default);
}
