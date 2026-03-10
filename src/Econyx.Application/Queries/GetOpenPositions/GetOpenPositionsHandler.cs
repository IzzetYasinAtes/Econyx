namespace Econyx.Application.Queries.GetOpenPositions;

using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;
using MediatR;

public sealed class GetOpenPositionsHandler : IRequestHandler<GetOpenPositionsQuery, IReadOnlyList<PositionDto>>
{
    private readonly IRepository<Position, Guid> _positionRepository;

    public GetOpenPositionsHandler(IRepository<Position, Guid> positionRepository)
    {
        _positionRepository = positionRepository;
    }

    public async Task<IReadOnlyList<PositionDto>> Handle(GetOpenPositionsQuery request, CancellationToken cancellationToken)
    {
        var positions = await _positionRepository.FindAsync(p => p.IsOpen, cancellationToken);

        return positions.Select(p => new PositionDto(
            p.Id,
            p.MarketId,
            p.MarketQuestion,
            p.Platform,
            p.Side,
            p.EntryPrice,
            p.CurrentPrice,
            p.Quantity,
            p.StrategyName,
            p.CalculatePnL(),
            p.CreatedAt)).ToList();
    }
}
