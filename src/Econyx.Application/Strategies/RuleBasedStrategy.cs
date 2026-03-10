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
            m.Spread <= _options.MaxSpreadCents / 100m);

        foreach (var market in eligible)
        {
            foreach (var outcome in market.Outcomes)
            {
                ct.ThrowIfCancellationRequested();

                var price = outcome.Price.Value;

                if (price < 0.15m)
                {
                    var buyEdge = 0.15m - price;

                    if (buyEdge >= _options.MinEdgeThreshold)
                    {
                        signals.Add(new StrategySignal(
                            market.Id,
                            market.Question,
                            TradeSide.Yes,
                            Edge.Create(buyEdge),
                            Probability.Create(0.15m),
                            outcome.Price,
                            0.6m,
                            Name,
                            $"Price {price:P1} below 15% threshold, potential value buy on '{outcome.Name}'"));
                    }
                }
                else if (price > 0.85m)
                {
                    var sellEdge = price - 0.85m;

                    if (sellEdge >= _options.MinEdgeThreshold)
                    {
                        signals.Add(new StrategySignal(
                            market.Id,
                            market.Question,
                            TradeSide.No,
                            Edge.Create(sellEdge),
                            Probability.Create(0.85m),
                            outcome.Price,
                            0.6m,
                            Name,
                            $"Price {price:P1} above 85% threshold, potential fade on '{outcome.Name}'"));
                    }
                }
            }
        }

        return Task.FromResult<IReadOnlyList<StrategySignal>>(signals);
    }
}
