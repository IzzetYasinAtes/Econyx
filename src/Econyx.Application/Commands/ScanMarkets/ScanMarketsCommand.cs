namespace Econyx.Application.Commands.ScanMarkets;

using Econyx.Application.Strategies;
using MediatR;

public sealed record ScanMarketsCommand : IRequest<ScanMarketsResult>;

public sealed record ScanMarketsResult(
    IReadOnlyList<StrategySignal> Signals,
    int MarketsScanned,
    int MarketsFiltered,
    decimal ApiCost,
    TimeSpan Duration);
