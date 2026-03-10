namespace Econyx.Application.Queries.GetTradeHistory;

using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;
using MediatR;

public sealed class GetTradeHistoryHandler : IRequestHandler<GetTradeHistoryQuery, TradeHistoryDto>
{
    private readonly IRepository<Trade, Guid> _tradeRepository;

    public GetTradeHistoryHandler(IRepository<Trade, Guid> tradeRepository)
    {
        _tradeRepository = tradeRepository;
    }

    public async Task<TradeHistoryDto> Handle(GetTradeHistoryQuery request, CancellationToken cancellationToken)
    {
        var allTrades = await _tradeRepository.GetAllAsync(cancellationToken);

        var ordered = allTrades
            .OrderByDescending(t => t.ClosedAt)
            .ToList();

        var paged = ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TradeDto(
                t.Id,
                t.MarketId,
                t.MarketQuestion,
                t.Side,
                t.EntryPrice,
                t.ExitPrice,
                t.Quantity,
                t.PnL,
                t.StrategyName,
                t.Platform,
                t.Duration,
                t.ClosedAt))
            .ToList();

        return new TradeHistoryDto(paged, ordered.Count);
    }
}
