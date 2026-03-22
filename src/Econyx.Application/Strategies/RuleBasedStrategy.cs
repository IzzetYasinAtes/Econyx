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

                if (price <= 0.03m || price >= 0.97m)
                    continue;

                if (price < 0.15m)
                {
                    var buyEdge = 0.15m - price;

                    if (buyEdge >= _options.MinEdgeThreshold)
                    {
                        var candidate = new StrategySignal(
                            market.Id,
                            market.Question,
                            outcome.Token.Value,
                            TradeSide.Yes,
                            Edge.Create(buyEdge),
                            Probability.Create(0.15m),
                            outcome.Price,
                            0.6m,
                            Name,
                            $"Price {price:P1} below 15% threshold, buying '{outcome.Name}' token");

                        if (bestSignal is null || candidate.Edge.AbsoluteValue > bestSignal.Edge.AbsoluteValue)
                            bestSignal = candidate;
                    }
                }
                else if (price > 0.85m)
                {
                    var complementary = market.Outcomes.FirstOrDefault(o =>
                        o.Token.Value != outcome.Token.Value);

                    if (complementary is null)
                        continue;

                    var compPrice = complementary.Price.Value;

                    if (compPrice <= 0.03m || compPrice >= 0.97m)
                        continue;

                    var buyEdge = 0.15m - compPrice;

                    if (buyEdge >= _options.MinEdgeThreshold)
                    {
                        var candidate = new StrategySignal(
                            market.Id,
                            market.Question,
                            complementary.Token.Value,
                            TradeSide.Yes,
                            Edge.Create(buyEdge),
                            Probability.Create(0.15m),
                            complementary.Price,
                            0.6m,
                            Name,
                            $"'{outcome.Name}' at {price:P1} overpriced, buying complementary '{complementary.Name}' token at {compPrice:P1}");

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
