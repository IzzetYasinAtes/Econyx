namespace Econyx.Infrastructure.Adapters.Polymarket;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using global::Polymarket.Net.Enums;
using global::Polymarket.Net.Interfaces.Clients;
using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;

internal sealed partial class PolymarketAdapter : IPlatformAdapter
{
    private readonly IPolymarketRestClient _client;
    private readonly ILogger<PolymarketAdapter> _logger;
    private readonly decimal _minVolumeUsd;

    public PolymarketAdapter(
        IPolymarketRestClient client,
        ILogger<PolymarketAdapter> logger,
        IOptions<TradingOptions> tradingOptions)
    {
        _client = client;
        _logger = logger;
        _minVolumeUsd = tradingOptions.Value.MinVolumeUsd;
    }

    public PlatformType Platform => PlatformType.Polymarket;

    public async Task<IReadOnlyList<Market>> GetMarketsAsync(CancellationToken ct = default)
    {
        const int pageSize = 500;
        var markets = new List<Market>();
        var offset = 0;

        while (!ct.IsCancellationRequested)
        {
            var result = await _client.GammaApi.GetEventsAsync(
                closed: false, active: true, volumeMin: _minVolumeUsd,
                orderBy: ["volume24hr"], ascending: false,
                limit: pageSize, offset: offset, ct: ct);

            if (!result.Success)
            {
                LogGetEventsFailed(_logger, result.Error?.ToString());
                break;
            }

            if (result.Data is null || result.Data.Length == 0)
                break;

            foreach (var evt in result.Data)
            {
                var mapped = PolymarketMapper.ToDomainMarket(evt);
                if (mapped is not null)
                    markets.Add(mapped);
            }

            if (result.Data.Length < pageSize)
                break;

            offset += pageSize;
        }

        LogMarketsFetched(_logger, markets.Count);
        return markets;
    }

    public async Task<MarketOrderBook> GetOrderBookAsync(string tokenId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenId);

        var result = await _client.ClobApi.ExchangeData.GetOrderBookAsync(tokenId, ct: ct);
        if (!result.Success)
        {
            LogOrderBookFailed(_logger, tokenId, result.Error?.ToString());
            return new MarketOrderBook(tokenId, [], [], 0m);
        }

        return PolymarketMapper.ToOrderBook(tokenId, result.Data);
    }

    public async Task<Probability?> GetPriceAsync(string tokenId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenId);

        var result = await _client.ClobApi.ExchangeData.GetPriceAsync(tokenId, OrderSide.Buy, ct: ct);
        if (!result.Success)
        {
            LogPriceFailed(_logger, tokenId, result.Error?.ToString());
            return null;
        }

        var price = Math.Clamp(result.Data.Price, 0m, 1m);
        return Probability.Create(price);
    }

    public Task<Money> GetBalanceAsync(CancellationToken ct = default)
    {
        LogBalanceNotImplemented(_logger);
        return Task.FromResult(Money.Zero());
    }

    public Task<string> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        LogPlaceOrderNotImplemented(_logger);
        throw new NotImplementedException(
            "Live order placement requires Polymarket CLOB signing integration. Use PaperTrading mode for now.");
    }

    public Task CancelOrderAsync(string platformOrderId, CancellationToken ct = default)
    {
        LogCancelOrderNotImplemented(_logger);
        throw new NotImplementedException(
            "Live order cancellation requires Polymarket CLOB signing integration. Use PaperTrading mode for now.");
    }

    public Task CreditBalanceAsync(decimal amount, CancellationToken ct = default)
        => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Polymarket GetEventsAsync failed: {Error}")]
    private static partial void LogGetEventsFailed(ILogger logger, string? error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetched {Count} markets from Polymarket")]
    private static partial void LogMarketsFetched(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Polymarket GetOrderBookAsync failed for {TokenId}: {Error}")]
    private static partial void LogOrderBookFailed(ILogger logger, string tokenId, string? error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Polymarket GetPriceAsync failed for {TokenId}: {Error}")]
    private static partial void LogPriceFailed(ILogger logger, string tokenId, string? error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "GetBalanceAsync not yet fully implemented for Polymarket live trading")]
    private static partial void LogBalanceNotImplemented(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "PlaceOrderAsync not yet implemented for Polymarket live trading")]
    private static partial void LogPlaceOrderNotImplemented(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "CancelOrderAsync not yet implemented for Polymarket live trading")]
    private static partial void LogCancelOrderNotImplemented(ILogger logger);
}
