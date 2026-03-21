namespace Econyx.Application.Commands.PlaceOrder;

using Econyx.Application.Ports;
using Econyx.Core.Interfaces;
using Econyx.Core.Primitives;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;
using Econyx.Domain.ValueObjects;
using MediatR;

public sealed class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Result<Guid>>
{
    private readonly IPlatformAdapter _platform;
    private readonly IOrderRepository _orderRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PlaceOrderHandler(
        IPlatformAdapter platform,
        IOrderRepository orderRepository,
        IPositionRepository positionRepository,
        IUnitOfWork unitOfWork)
    {
        _platform = platform;
        _orderRepository = orderRepository;
        _positionRepository = positionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var signal = request.Signal;
        if (signal.MarketPrice.Value <= 0)
        {
            return Result.Failure<Guid>(
                Error.Validation("Market price must be greater than zero to place an order."));
        }

        var marketPrice = Money.Create(signal.MarketPrice.Value);
        var quantity = request.PositionSize.Amount / signal.MarketPrice.Value;

        var order = Order.Create(
            signal.MarketId,
            signal.TokenId,
            signal.RecommendedSide,
            OrderType.Limit,
            marketPrice,
            quantity,
            request.Mode,
            _platform.Platform);

        if (request.Mode == TradingMode.Live)
        {
            try
            {
                var platformOrderId = await _platform.PlaceOrderAsync(
                    new PlaceOrderRequest(
                        signal.TokenId,
                        signal.RecommendedSide,
                        signal.MarketPrice.Value,
                        order.Quantity,
                        OrderType.Limit),
                    cancellationToken);

                order.SetPlatformOrderId(platformOrderId);
                order.Fill(marketPrice, order.Quantity);
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
            order.Fill(marketPrice, order.Quantity);
        }

        var position = Position.Create(
            signal.MarketId,
            signal.MarketQuestion,
            signal.TokenId,
            _platform.Platform,
            signal.RecommendedSide,
            marketPrice,
            order.Quantity,
            signal.StrategyName);

        await _orderRepository.AddAsync(order, cancellationToken);
        await _positionRepository.AddAsync(position, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(position.Id);
    }
}
