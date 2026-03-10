namespace Econyx.Application.Strategies;

using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using Microsoft.Extensions.Options;

public sealed class AiAnalysisStrategy : IStrategy
{
    private readonly IAiAnalysisService _aiService;
    private readonly TradingOptions _options;

    public AiAnalysisStrategy(IAiAnalysisService aiService, IOptions<TradingOptions> options)
    {
        _aiService = aiService;
        _options = options.Value;
    }

    public string Name => "AiAnalysis";

    public async Task<IReadOnlyList<StrategySignal>> EvaluateAsync(
        IReadOnlyList<Market> markets,
        CancellationToken ct = default)
    {
        var signals = new List<StrategySignal>();

        foreach (var market in markets)
        {
            ct.ThrowIfCancellationRequested();

            if (market.Status != MarketStatus.Open)
                continue;

            var request = new MarketAnalysisRequest(
                market.Question,
                market.Description,
                market.Category,
                market.Outcomes.Select(o => o.Name).ToList(),
                market.Outcomes.Select(o => o.Price.Value).ToList(),
                market.VolumeUsd);

            var result = await _aiService.AnalyzeMarketAsync(request, ct);

            foreach (var outcome in result.Outcomes)
            {
                var marketOutcome = market.Outcomes
                    .FirstOrDefault(o => o.Name == outcome.OutcomeName);

                if (marketOutcome is null)
                    continue;

                var edgeValue = outcome.FairValue.Value - marketOutcome.Price.Value;
                var edge = Edge.Create(Math.Abs(edgeValue));

                if (!edge.IsActionable(_options.MinEdgeThreshold))
                    continue;

                var side = edgeValue > 0 ? TradeSide.Yes : TradeSide.No;

                signals.Add(new StrategySignal(
                    market.Id,
                    market.Question,
                    side,
                    edge,
                    outcome.FairValue,
                    marketOutcome.Price,
                    result.Confidence,
                    Name,
                    result.Reasoning));
            }
        }

        return signals;
    }
}
