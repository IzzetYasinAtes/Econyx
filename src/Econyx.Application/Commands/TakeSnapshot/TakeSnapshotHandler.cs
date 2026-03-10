namespace Econyx.Application.Commands.TakeSnapshot;

using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Core.Interfaces;
using Econyx.Core.Primitives;
using Econyx.Domain.Entities;
using Econyx.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Options;

public sealed class TakeSnapshotHandler : IRequestHandler<TakeSnapshotCommand, Result<Guid>>
{
    private readonly IPlatformAdapter _platform;
    private readonly IRepository<Position, Guid> _positionRepository;
    private readonly IRepository<Trade, Guid> _tradeRepository;
    private readonly IRepository<BalanceSnapshot, Guid> _snapshotRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TradingOptions _options;

    public TakeSnapshotHandler(
        IPlatformAdapter platform,
        IRepository<Position, Guid> positionRepository,
        IRepository<Trade, Guid> tradeRepository,
        IRepository<BalanceSnapshot, Guid> snapshotRepository,
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
        var balance = await _platform.GetBalanceAsync(cancellationToken);
        var openPositions = await _positionRepository.FindAsync(p => p.IsOpen, cancellationToken);
        var allTrades = await _tradeRepository.GetAllAsync(cancellationToken);

        var totalPnL = allTrades.Count > 0
            ? allTrades.Aggregate(Money.Zero(), (sum, t) => sum + t.PnL)
            : Money.Zero();

        var initialBalance = Money.Create(_options.InitialBalance);
        var totalPnLPercent = initialBalance.Amount > 0
            ? totalPnL.Amount / initialBalance.Amount * 100m
            : 0m;

        var winCount = allTrades.Count(t => t.PnL.Amount > 0);
        var winRate = allTrades.Count > 0 ? (decimal)winCount / allTrades.Count : 0m;

        var snapshot = BalanceSnapshot.Create(
            balance,
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
