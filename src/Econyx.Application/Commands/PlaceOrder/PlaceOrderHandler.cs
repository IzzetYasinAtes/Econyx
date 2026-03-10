namespace Econyx.Application.Commands.PlaceOrder;

using Econyx.Application.Ports;
using Econyx.Core.Interfaces;
using Econyx.Core.Primitives;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using MediatR;

public sealed class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Result<Guid>>
{
    private readonly IPlatformAdapter _platform;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PlaceOrderHandler(
        IPlatformAdapter platform,
        IRepository<Order, Guid> orderRepository,
        IUnitOfWork unitOfWork)
    {
        _platform = platform;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var signal = request.Signal;

        var order = Order.Create(
            signal.MarketId,
            signal.RecommendedSide,
            OrderType.Limit,
            request.PositionSize,
            request.PositionSize.Amount / signal.MarketPrice.Value,
            request.Mode,
            _platform.Platform);

        if (request.Mode == TradingMode.Live)
        {
            try
            {
                var platformOrderId = await _platform.PlaceOrderAsync(
                    new PlaceOrderRequest(
                        string.Empty,
                        signal.RecommendedSide,
                        signal.MarketPrice.Value,
                        order.Quantity,
                        OrderType.Limit),
                    cancellationToken);

                order.Fill(Money.Create(signal.MarketPrice.Value), order.Quantity);
            }
            catch (Exception ex)
            {
                order.Reject(ex.Message);
                await _orderRepository.AddAsync(order, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Failure<Guid>(Error.Failure($"Platform order failed: {ex.Message}"));
            }
        }
        else
        {
            order.Fill(Money.Create(signal.MarketPrice.Value), order.Quantity);
        }

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(order.Id);
    }
}
