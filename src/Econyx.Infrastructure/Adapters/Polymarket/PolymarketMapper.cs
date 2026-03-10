namespace Econyx.Infrastructure.Adapters.Polymarket;

using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using Econyx.Application.Ports;
using global::Polymarket.Net.Objects.Models;

internal static class PolymarketMapper
{
    public static Market? ToDomainMarket(PolymarketEvent evt)
    {
        if (evt.Markets is null || evt.Markets.Length == 0)
            return null;

        var outcomes = new List<MarketOutcome>();
        foreach (var mkt in evt.Markets)
        {
            if (mkt.ClobTokenIds is null || mkt.OutcomePrices is null)
                continue;

            for (var i = 0; i < mkt.ClobTokenIds.Length; i++)
            {
                var tokenId = mkt.ClobTokenIds[i];
                if (string.IsNullOrWhiteSpace(tokenId))
                    continue;

                var price = i < mkt.OutcomePrices.Length
                    ? Math.Clamp(mkt.OutcomePrices[i], 0m, 1m)
                    : 0m;
                var name = mkt.Outcomes is not null && i < mkt.Outcomes.Length
                    ? mkt.Outcomes[i]
                    : $"Outcome_{i}";

                outcomes.Add(MarketOutcome.Create(
                    name,
                    Probability.Create(price),
                    TokenId.Create(tokenId)));
            }
        }

        if (outcomes.Count == 0)
            return null;

        var primaryMarket = evt.Markets[0];
        var volume = primaryMarket.Volume24hr != 0 ? primaryMarket.Volume24hr : evt.Volume24hr;
        var spread = primaryMarket.Spread;

        return Market.Create(
            externalId: !string.IsNullOrEmpty(evt.Id) ? evt.Id : (primaryMarket.ConditionId ?? Guid.NewGuid().ToString()),
            platform: PlatformType.Polymarket,
            question: evt.Title ?? primaryMarket.Question ?? "Unknown",
            description: evt.Description ?? string.Empty,
            category: evt.Category ?? string.Empty,
            outcomes: outcomes,
            volumeUsd: volume,
            spread: spread,
            resolutionDate: evt.EndDate != default ? evt.EndDate : null);
    }

    public static MarketOrderBook ToOrderBook(string tokenId, PolymarketOrderBook book)
    {
        var bids = book.Bids?
            .Select(b => new OrderBookLevel(b.Price, b.Quantity))
            .ToList() ?? [];

        var asks = book.Asks?
            .Select(a => new OrderBookLevel(a.Price, a.Quantity))
            .ToList() ?? [];

        var bestBid = bids.Count > 0 ? bids.Max(b => b.Price) : 0m;
        var bestAsk = asks.Count > 0 ? asks.Min(a => a.Price) : 1m;
        var spread = bestAsk - bestBid;

        return new MarketOrderBook(tokenId, bids, asks, spread);
    }
}
