namespace Econyx.Infrastructure.Adapters;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using Econyx.Infrastructure.Adapters.Polymarket;

internal sealed partial class PaperTradingAdapter : IPlatformAdapter, IDisposable
{
    private readonly PolymarketAdapter _dataSource;
    private readonly ILogger<PaperTradingAdapter> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private decimal _balance;
    private int _orderCounter;

    public PaperTradingAdapter(
        IOptions<TradingOptions> tradingOptions,
        ILogger<PaperTradingAdapter> logger,
        PolymarketAdapter dataSource)
    {
        _dataSource = dataSource;
        _logger = logger;
        _balance = tradingOptions.Value.InitialBalance;
    }

    public PlatformType Platform => PlatformType.Polymarket;

    public Task<IReadOnlyList<Market>> GetMarketsAsync(CancellationToken ct = default)
        => _dataSource.GetMarketsAsync(ct);

    public Task<MarketOrderBook> GetOrderBookAsync(string tokenId, CancellationToken ct = default)
        => _dataSource.GetOrderBookAsync(tokenId, ct);

    public Task<Probability> GetPriceAsync(string tokenId, CancellationToken ct = default)
        => _dataSource.GetPriceAsync(tokenId, ct);

    public async Task<Money> GetBalanceAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return Money.Create(_balance);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _lock.WaitAsync(ct);
        try
        {
            var cost = request.Price * request.Quantity;

            if (cost > _balance)
            {
                LogInsufficientBalance(_logger, cost, _balance);
                throw new InvalidOperationException(
                    $"Insufficient paper balance. Required: {cost:F2}, Available: {_balance:F2}");
            }

            _balance -= cost;
            _orderCounter++;
            var orderId = $"PAPER-{_orderCounter:D6}";

            LogOrderPlaced(_logger, orderId, request.Side, request.Quantity, request.Price, _balance);

            return orderId;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task CancelOrderAsync(string platformOrderId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformOrderId);

        await _lock.WaitAsync(ct);
        try
        {
            LogOrderCancelled(_logger, platformOrderId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task CreditBalanceAsync(decimal amount, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _balance += amount;
            LogBalanceCredited(_logger, amount, _balance);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();

    [LoggerMessage(Level = LogLevel.Warning, Message = "[Paper] Insufficient balance. Required: {Cost}, Available: {Balance}")]
    private static partial void LogInsufficientBalance(ILogger logger, decimal cost, decimal balance);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Paper] Order placed: {OrderId} | {Side} {Quantity}x @ {Price} | Balance: {Balance}")]
    private static partial void LogOrderPlaced(ILogger logger, string orderId, Domain.Enums.TradeSide side, decimal quantity, decimal price, decimal balance);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Paper] Order cancelled: {OrderId}")]
    private static partial void LogOrderCancelled(ILogger logger, string orderId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Paper] Balance credited: +{Amount} | Balance: {Balance}")]
    private static partial void LogBalanceCredited(ILogger logger, decimal amount, decimal balance);
}
