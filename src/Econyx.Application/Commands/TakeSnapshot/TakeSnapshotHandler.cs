namespace Econyx.Application.Commands.TakeSnapshot;

using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Application.Services;
using Econyx.Core.Interfaces;
using Econyx.Core.Primitives;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;
using Econyx.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Options;

public sealed class TakeSnapshotHandler : IRequestHandler<TakeSnapshotCommand, Result<Guid>>
{
    private readonly IPlatformAdapter _platform;
    private readonly IPositionRepository _positionRepository;
    private readonly ITradeRepository _tradeRepository;
    private readonly IBalanceSnapshotRepository _snapshotRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TradingOptions _options;

    public TakeSnapshotHandler(
        IPlatformAdapter platform,
        IPositionRepository positionRepository,
        ITradeRepository tradeRepository,
        IBalanceSnapshotRepository snapshotRepository,
        IUnitOfWork unitOfWork,
        IOptions<TradingOptions> options)
    {
        _platform = platform;
        _positionRepository = positionRepository;
        _tradeRepository = tradeRepository;
        _snapshotRepository = snapshotRepository;
        _unitOfWork = unitOfWork;
        _options = options.Value;
    }

    public async Task<Result<Guid>> Handle(TakeSnapshotCommand request, CancellationToken cancellationToken)
    {
        var cashBalance = await _platform.GetBalanceAsync(cancellationToken);
        var openPositions = await _positionRepository.FindAsync(p => p.IsOpen, cancellationToken);
        var allTrades = await _tradeRepository.GetAllAsync(cancellationToken);

        var (totalPnL, winRate) = TradeStatsCalculator.Calculate(allTrades);

        var openPositionsValue = 0m;
        foreach (var pos in openPositions)
            openPositionsValue += pos.CurrentPrice.Amount * pos.Quantity;

        var totalPortfolio = Money.Create(cashBalance.Amount + openPositionsValue);

        var initialBalance = Money.Create(_options.InitialBalance);
        var totalPnLPercent = initialBalance.Amount > 0
            ? totalPnL.Amount / initialBalance.Amount * 100m
            : 0m;

        var snapshot = BalanceSnapshot.Create(
            totalPortfolio,
            totalPnL,
            totalPnLPercent,
            openPositions.Count,
            allTrades.Count,
            winRate,
            Money.Zero(),
            _options.Mode);

        await _snapshotRepository.AddAsync(snapshot, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(snapshot.Id);
    }
}
