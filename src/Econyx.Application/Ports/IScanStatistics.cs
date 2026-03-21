namespace Econyx.Application.Ports;

public interface IScanStatistics
{
    int TotalMarketsScanned { get; }
    void RecordScan(int marketsScanned);
}
