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
            m.Outcomes.Count == 2 &&
            (m.ResolutionDate == null || m.ResolutionDate <= DateTime.UtcNow.AddDays(30)));

        foreach (var market in eligible)
        {
            ct.ThrowIfCancellationRequested();

            var isShortTerm = market.ResolutionDate.HasValue &&
                              market.ResolutionDate.Value <= DateTime.UtcNow.AddHours(2);

            var lowMin = isShortTerm ? 0.10m : 0.15m;
            var lowMax = isShortTerm ? 0.48m : 0.45m;
            var highMin = isShortTerm ? 0.52m : 0.55m;
            var highMax = isShortTerm ? 0.90m : 0.85m;

            StrategySignal? bestSignal = null;

            foreach (var outcome in market.Outcomes)
            {
                var price = outcome.Price.Value;

                if (price >= lowMin && price <= lowMax)
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

                else if (price >= highMin && price <= highMax)
                {
                    var complementary = market.Outcomes.FirstOrDefault(o =>
                        o.Token.Value != outcome.Token.Value);

                    if (complementary is null)
                        continue;

                    var compPrice = complementary.Price.Value;


                    if (compPrice < lowMin || compPrice > lowMax)
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
