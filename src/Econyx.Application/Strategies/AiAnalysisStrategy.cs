namespace Econyx.Application.Strategies;

using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using Microsoft.Extensions.Options;

public sealed class AiAnalysisStrategy : IStrategy
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly TradingOptions _options;

    public AiAnalysisStrategy(IAiProviderFactory providerFactory, IOptions<TradingOptions> options)
    {
        _providerFactory = providerFactory;
        _options = options.Value;
    }

    public string Name => "AiAnalysis";

    public async Task<IReadOnlyList<StrategySignal>> EvaluateAsync(
        IReadOnlyList<Market> markets,
        CancellationToken ct = default)
    {
        var signals = new List<StrategySignal>();

        var aiService = await _providerFactory.GetProviderAsync(ct);

        foreach (var market in markets)
        {
            ct.ThrowIfCancellationRequested();

            if (market.Status != MarketStatus.Open || market.Outcomes.Count != 2)
                continue;

            var request = new MarketAnalysisRequest(
                market.Question,
                market.Description,
                market.Category,
                market.Outcomes.Select(o => o.Name).ToList(),
                market.Outcomes.Select(o => o.Price.Value).ToList(),
                market.VolumeUsd);

            var result = await aiService.AnalyzeMarketAsync(request, ct);

            StrategySignal? bestSignal = null;

            foreach (var outcome in result.Outcomes)
            {
                var marketOutcome = market.Outcomes
                    .FirstOrDefault(o => o.Name == outcome.OutcomeName);

                if (marketOutcome is null)
                    continue;

                var edgeValue = outcome.FairValue.Value - marketOutcome.Price.Value;
                var absEdge = Math.Abs(edgeValue);

                if (absEdge < _options.MinEdgeThreshold)
                    continue;

                string tokenId;
                decimal entryPrice;

                if (edgeValue > 0)
                {

                    tokenId = marketOutcome.Token.Value;
                    entryPrice = marketOutcome.Price.Value;
                }
                else
                {

                    var comp = market.Outcomes.FirstOrDefault(o =>
                        o.Token.Value != marketOutcome.Token.Value);
                    if (comp is null) continue;
                    tokenId = comp.Token.Value;
                    entryPrice = comp.Price.Value;
                }

                var candidate = new StrategySignal(
                    market.Id,
                    market.Question,
                    tokenId,
                    TradeSide.Yes,
                    Edge.Create(absEdge),
                    outcome.FairValue,
                    Probability.Create(entryPrice),
                    result.Confidence,
                    Name,
                    result.Reasoning);

                if (bestSignal is null || absEdge > bestSignal.Edge.AbsoluteValue)
                    bestSignal = candidate;
            }

            if (bestSignal is not null)
                signals.Add(bestSignal);
        }

        return signals;
    }
}
