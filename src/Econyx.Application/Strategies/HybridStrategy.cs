namespace Econyx.Application.Strategies;

using Econyx.Application.Configuration;
using Econyx.Domain.Entities;
using Microsoft.Extensions.Options;

public sealed class HybridStrategy : IStrategy
{
    private readonly RuleBasedStrategy _ruleStrategy;
    private readonly AiAnalysisStrategy _aiStrategy;
    private readonly TradingOptions _options;

    public HybridStrategy(
        RuleBasedStrategy ruleStrategy,
        AiAnalysisStrategy aiStrategy,
        IOptions<TradingOptions> options)
    {
        _ruleStrategy = ruleStrategy;
        _aiStrategy = aiStrategy;
        _options = options.Value;
    }

    public string Name => "Hybrid";

    public async Task<IReadOnlyList<StrategySignal>> EvaluateAsync(
        IReadOnlyList<Market> markets,
        CancellationToken ct = default)
    {
        var ruleSignals = await _ruleStrategy.EvaluateAsync(markets, ct);

        if (ruleSignals.Count == 0)
            return ruleSignals;

        var filteredMarketIds = ruleSignals
            .OrderByDescending(s => s.Edge.AbsoluteValue)
            .Select(s => s.MarketId)
            .Distinct()
            .Take(_options.MaxAiCandidates)
            .ToHashSet();

        var filteredMarkets = markets
            .Where(m => filteredMarketIds.Contains(m.Id))
            .ToList();

        var aiSignals = await _aiStrategy.EvaluateAsync(filteredMarkets, ct);

        var combined = new List<StrategySignal>();

        foreach (var aiSignal in aiSignals)
        {
            var matchingRule = ruleSignals.FirstOrDefault(r =>
                r.MarketId == aiSignal.MarketId);

            if (matchingRule is null)
                continue;

            combined.Add(aiSignal with
            {
                StrategyName = Name,
                Confidence = (matchingRule.Confidence + aiSignal.Confidence) / 2m,
                Reasoning = $"[Rule] {matchingRule.Reasoning} | [AI] {aiSignal.Reasoning}"
            });
        }

        return combined;
    }
}
