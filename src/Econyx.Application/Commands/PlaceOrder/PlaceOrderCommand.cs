namespace Econyx.Application.Commands.PlaceOrder;

using Econyx.Application.Strategies;
using Econyx.Core.Primitives;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using MediatR;

public sealed record PlaceOrderCommand(
    StrategySignal Signal,
    Money PositionSize,
    TradingMode Mode) : IRequest<Result<Guid>>;
