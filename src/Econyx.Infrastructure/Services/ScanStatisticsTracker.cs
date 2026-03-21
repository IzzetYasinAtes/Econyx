namespace Econyx.Infrastructure.Services;

using Econyx.Application.Ports;

internal sealed class ScanStatisticsTracker : IScanStatistics
{
    private int _totalMarketsScanned;

    public int TotalMarketsScanned => _totalMarketsScanned;

    public void RecordScan(int marketsScanned)
        => Interlocked.Add(ref _totalMarketsScanned, marketsScanned);
}
