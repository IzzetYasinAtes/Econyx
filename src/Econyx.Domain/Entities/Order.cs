using Econyx.Core.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.Events;
using Econyx.Domain.ValueObjects;

namespace Econyx.Domain.Entities;

public sealed class Order : BaseEntity<Guid>
{
    public Guid MarketId { get; private set; }
    public string? PlatformOrderId { get; private set; }
    public TradeSide Side { get; private set; }
    public OrderType Type { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money Price { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public Money? FilledPrice { get; private set; }
    public decimal? FilledQuantity { get; private set; }
    public TradingMode Mode { get; private set; }
    public PlatformType Platform { get; private set; }
    public DateTime? FilledAt { get; private set; }
    public string? RejectionReason { get; private set; }

    private Order() { }

    public static Order Create(
        Guid marketId,
        TradeSide side,
        OrderType type,
        Money price,
        decimal quantity,
        TradingMode mode,
        PlatformType platform,
        string? platformOrderId = null)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            MarketId = marketId,
            PlatformOrderId = platformOrderId,
            Side = side,
            Type = type,
            Status = OrderStatus.Pending,
            Price = price,
            Quantity = quantity,
            Mode = mode,
            Platform = platform
        };

        order.RaiseDomainEvent(new OrderPlacedEvent(order.Id, marketId, side, price, quantity, mode));
        return order;
    }

    public void Fill(Money price, decimal quantity)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (Status is not (OrderStatus.Pending or OrderStatus.PartiallyFilled))
            throw new InvalidOperationException($"Cannot fill order in {Status} status.");

        FilledPrice = price;
        FilledQuantity = (FilledQuantity ?? 0) + quantity;
        FilledAt = DateTime.UtcNow;

        Status = FilledQuantity >= Quantity
            ? OrderStatus.Filled
            : OrderStatus.PartiallyFilled;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string? reason = null)
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.PartiallyFilled))
            throw new InvalidOperationException($"Cannot cancel order in {Status} status.");

        Status = OrderStatus.Cancelled;
        RejectionReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPlatformOrderId(string platformOrderId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformOrderId);
        PlatformOrderId = platformOrderId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot reject order in {Status} status.");

        Status = OrderStatus.Rejected;
        RejectionReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}
