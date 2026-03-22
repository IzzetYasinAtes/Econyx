namespace Econyx.Application.Ports;

using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;

public interface IPlatformAdapter
{
    PlatformType Platform { get; }
    Task<IReadOnlyList<Market>> GetMarketsAsync(CancellationToken ct = default);
    Task<MarketOrderBook> GetOrderBookAsync(string tokenId, CancellationToken ct = default);
    Task<Probability?> GetPriceAsync(string tokenId, CancellationToken ct = default);
    Task<Money> GetBalanceAsync(CancellationToken ct = default);
    Task<string> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default);
    Task CancelOrderAsync(string platformOrderId, CancellationToken ct = default);
    Task CreditBalanceAsync(decimal amount, CancellationToken ct = default);
}

public record MarketOrderBook(
    string TokenId,
    IReadOnlyList<OrderBookLevel> Bids,
    IReadOnlyList<OrderBookLevel> Asks,
    decimal Spread);

public record OrderBookLevel(decimal Price, decimal Size);

public record PlaceOrderRequest(
    string TokenId,
    TradeSide Side,
    decimal Price,
    decimal Quantity,
    OrderType Type);
