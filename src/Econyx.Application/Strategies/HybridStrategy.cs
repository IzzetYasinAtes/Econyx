namespace Econyx.Application.Strategies;

using Econyx.Domain.Entities;

public sealed class HybridStrategy : IStrategy
{
    private readonly RuleBasedStrategy _ruleStrategy;
    private readonly AiAnalysisStrategy _aiStrategy;

    public HybridStrategy(RuleBasedStrategy ruleStrategy, AiAnalysisStrategy aiStrategy)
    {
        _ruleStrategy = ruleStrategy;
        _aiStrategy = aiStrategy;
    }

    public string Name => "Hybrid";

    public async Task<IReadOnlyList<StrategySignal>> EvaluateAsync(
        IReadOnlyList<Market> markets,
        CancellationToken ct = default)
    {
        var ruleSignals = await _ruleStrategy.EvaluateAsync(markets, ct);

        if (ruleSignals.Count == 0)
            return ruleSignals;

        var filteredMarketIds = ruleSignals.Select(s => s.MarketId).ToHashSet();
        var filteredMarkets = markets.Where(m => filteredMarketIds.Contains(m.Id)).ToList();

        var aiSignals = await _aiStrategy.EvaluateAsync(filteredMarkets, ct);

        var combined = new List<StrategySignal>();

        foreach (var aiSignal in aiSignals)
        {
            var matchingRule = ruleSignals.FirstOrDefault(r =>
                r.MarketId == aiSignal.MarketId &&
                r.RecommendedSide == aiSignal.RecommendedSide);

            if (matchingRule is not null)
            {
                combined.Add(aiSignal with
                {
                    StrategyName = Name,
                    Confidence = (matchingRule.Confidence + aiSignal.Confidence) / 2m,
                    Reasoning = $"[Rule] {matchingRule.Reasoning} | [AI] {aiSignal.Reasoning}"
                });
            }
            else
            {
                combined.Add(aiSignal with { StrategyName = Name });
            }
        }

        return combined;
    }
}
