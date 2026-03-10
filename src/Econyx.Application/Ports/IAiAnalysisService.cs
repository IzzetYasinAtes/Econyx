namespace Econyx.Application.Ports;

using Econyx.Domain.ValueObjects;

public interface IAiAnalysisService
{
    string ProviderName { get; }
    Task<FairValueResult> AnalyzeMarketAsync(MarketAnalysisRequest request, CancellationToken ct = default);
}

public record MarketAnalysisRequest(
    string Question,
    string Description,
    string Category,
    IReadOnlyList<string> OutcomeNames,
    IReadOnlyList<decimal> CurrentPrices,
    decimal VolumeUsd);

public record FairValueResult(
    IReadOnlyList<OutcomeFairValue> Outcomes,
    decimal Confidence,
    string Reasoning,
    decimal ApiCostUsd);

public record OutcomeFairValue(string OutcomeName, Probability FairValue);
