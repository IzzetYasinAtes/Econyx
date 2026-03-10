namespace Econyx.Application.Ports;

public interface IAiProviderFactory
{
    Task<IAiAnalysisService> GetProviderAsync(CancellationToken ct = default);
}
