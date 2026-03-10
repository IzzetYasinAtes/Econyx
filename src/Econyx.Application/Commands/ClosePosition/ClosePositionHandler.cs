namespace Econyx.Application.Commands.ClosePosition;

using Econyx.Core.Interfaces;
using Econyx.Core.Primitives;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;
using Econyx.Domain.ValueObjects;
using MediatR;

public sealed class ClosePositionHandler : IRequestHandler<ClosePositionCommand, Result<Money>>
{
    private readonly IPositionRepository _positionRepository;
    private readonly ITradeRepository _tradeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ClosePositionHandler(
        IPositionRepository positionRepository,
        ITradeRepository tradeRepository,
        IUnitOfWork unitOfWork)
    {
        _positionRepository = positionRepository;
        _tradeRepository = tradeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Money>> Handle(ClosePositionCommand request, CancellationToken cancellationToken)
    {
        var position = await _positionRepository.GetByIdAsync(request.PositionId, cancellationToken);

        if (position is null)
            return Result.Failure<Money>(Error.NotFound(nameof(Position), request.PositionId));

        if (!position.IsOpen)
            return Result.Failure<Money>(Error.Conflict("Position is already closed."));

        position.Close(request.ExitPrice);

        var pnl = position.CalculatePnL();
        var duration = (position.ClosedAt ?? DateTime.UtcNow) - position.CreatedAt;

        var trade = Trade.Create(
            position.Id,
            position.MarketId,
            position.MarketQuestion,
            position.Side,
            position.EntryPrice,
            request.ExitPrice,
            position.Quantity,
            pnl,
            Money.Zero(),
            position.StrategyName,
            position.Platform,
            duration);

        _positionRepository.Update(position);
        await _tradeRepository.AddAsync(trade, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(pnl);
    }
}
