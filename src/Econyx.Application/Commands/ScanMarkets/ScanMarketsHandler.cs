namespace Econyx.Application.Commands.ScanMarkets;

using System.Diagnostics;
using Econyx.Application.Ports;
using Econyx.Application.Strategies;
using MediatR;

public sealed class ScanMarketsHandler : IRequestHandler<ScanMarketsCommand, ScanMarketsResult>
{
    private readonly IPlatformAdapter _platform;
    private readonly IStrategy _strategy;

    public ScanMarketsHandler(IPlatformAdapter platform, IStrategy strategy)
    {
        _platform = platform;
        _strategy = strategy;
    }

    public async Task<ScanMarketsResult> Handle(ScanMarketsCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var markets = await _platform.GetMarketsAsync(cancellationToken);
        var signals = await _strategy.EvaluateAsync(markets, cancellationToken);

        sw.Stop();

        return new ScanMarketsResult(
            signals,
            markets.Count,
            signals.Count,
            0m,
            sw.Elapsed);
    }
}
