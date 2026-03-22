namespace Econyx.Application.Queries.GetBalanceHistory;

using Econyx.Domain.Repositories;
using MediatR;

public sealed class GetBalanceHistoryHandler : IRequestHandler<GetBalanceHistoryQuery, IReadOnlyList<BalanceHistoryPoint>>
{
    private readonly IBalanceSnapshotRepository _snapshotRepository;

    public GetBalanceHistoryHandler(IBalanceSnapshotRepository snapshotRepository)
    {
        _snapshotRepository = snapshotRepository;
    }

    public async Task<IReadOnlyList<BalanceHistoryPoint>> Handle(
        GetBalanceHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.AddHours(-48);
        var until = DateTime.UtcNow;

        var snapshots = await _snapshotRepository.GetHistoryAsync(from, until, cancellationToken);

        return snapshots
            .OrderBy(s => s.CreatedAt)
            .Select(s => new BalanceHistoryPoint(s.CreatedAt, s.Balance.Amount))
            .ToList();
    }
}
