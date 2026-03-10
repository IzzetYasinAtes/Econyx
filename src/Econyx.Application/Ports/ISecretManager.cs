namespace Econyx.Application.Ports;

public interface ISecretManager
{
    Task<string> GetSecretAsync(string name, CancellationToken ct = default);
    Task SetSecretAsync(string name, string value, CancellationToken ct = default);
}
