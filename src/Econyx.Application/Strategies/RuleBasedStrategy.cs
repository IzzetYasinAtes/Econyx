namespace Econyx.Application.Strategies;

using Econyx.Application.Configuration;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using Microsoft.Extensions.Options;

public sealed class RuleBasedStrategy : IStrategy
{
    private readonly TradingOptions _options;

    public RuleBasedStrategy(IOptions<TradingOptions> options)
    {
        _options = options.Value;
    }

    public string Name => "RuleBased";

    public Task<IReadOnlyList<StrategySignal>> EvaluateAsync(
        IReadOnlyList<Market> markets,
        CancellationToken ct = default)
    {
        var signals = new List<StrategySignal>();

        var eligible = markets.Where(m =>
            m.Status == MarketStatus.Open &&
            m.VolumeUsd >= _options.MinVolumeUsd &&
            m.Spread <= _options.MaxSpreadCents / 100m &&
            m.Outcomes.Count == 2);

        foreach (var market in eligible)
        {
            ct.ThrowIfCancellationRequested();

            StrategySignal? bestSignal = null;

            foreach (var outcome in market.Outcomes)
            {
                var price = outcome.Price.Value;


                if (price >= 0.15m && price <= 0.45m)
                {
                    var edge = 0.50m - price;

                    if (edge >= _options.MinEdgeThreshold)
                    {
                        var candidate = new StrategySignal(
                            market.Id,
                            market.Question,
                            outcome.Token.Value,
                            TradeSide.Yes,
                            Edge.Create(edge),
                            Probability.Create(0.50m),
                            outcome.Price,
                            0.5m,
                            Name,
                            $"Token '{outcome.Name}' at {price:P1} in underpriced zone, potential buy");

                        if (bestSignal is null || candidate.Edge.AbsoluteValue > bestSignal.Edge.AbsoluteValue)
                            bestSignal = candidate;
                    }
                }

                else if (price >= 0.55m && price <= 0.85m)
                {
                    var complementary = market.Outcomes.FirstOrDefault(o =>
                        o.Token.Value != outcome.Token.Value);

                    if (complementary is null)
                        continue;

                    var compPrice = complementary.Price.Value;


                    if (compPrice < 0.15m || compPrice > 0.45m)
                        continue;

                    var edge = 0.50m - compPrice;

                    if (edge >= _options.MinEdgeThreshold)
                    {
                        var candidate = new StrategySignal(
                            market.Id,
                            market.Question,
                            complementary.Token.Value,
                            TradeSide.Yes,
                            Edge.Create(edge),
                            Probability.Create(0.50m),
                            complementary.Price,
                            0.5m,
                            Name,
                            $"'{outcome.Name}' at {price:P1} overpriced, buying '{complementary.Name}' at {compPrice:P1}");

                        if (bestSignal is null || candidate.Edge.AbsoluteValue > bestSignal.Edge.AbsoluteValue)
                            bestSignal = candidate;
                    }
                }
            }

            if (bestSignal is not null)
                signals.Add(bestSignal);
        }

        return Task.FromResult<IReadOnlyList<StrategySignal>>(signals);
    }
}
