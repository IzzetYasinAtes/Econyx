namespace Econyx.Application.Queries.GetBalanceHistory;

using MediatR;

public sealed record GetBalanceHistoryQuery : IRequest<IReadOnlyList<BalanceHistoryPoint>>;

public sealed record BalanceHistoryPoint(DateTime Timestamp, decimal Balance);
