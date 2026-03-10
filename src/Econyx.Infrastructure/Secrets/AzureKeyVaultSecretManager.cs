namespace Econyx.Infrastructure.Secrets;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Econyx.Application.Ports;

internal sealed partial class AzureKeyVaultSecretManager : ISecretManager
{
    private readonly SecretClient _client;
    private readonly ILogger<AzureKeyVaultSecretManager> _logger;

    public AzureKeyVaultSecretManager(IConfiguration configuration, ILogger<AzureKeyVaultSecretManager> logger)
    {
        var vaultUri = configuration["Azure:KeyVault:VaultUri"]
            ?? throw new InvalidOperationException("Azure:KeyVault:VaultUri is not configured.");

        _client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
        _logger = logger;
    }

    public async Task<string> GetSecretAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        LogRetrievingSecret(_logger, name);

        var response = await _client.GetSecretAsync(name, cancellationToken: ct);
        return response.Value.Value;
    }

    public async Task SetSecretAsync(string name, string value, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        LogSettingSecret(_logger, name);

        await _client.SetSecretAsync(name, value, ct);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Retrieving secret '{Name}' from Azure Key Vault")]
    private static partial void LogRetrievingSecret(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Information, Message = "Setting secret '{Name}' in Azure Key Vault")]
    private static partial void LogSettingSecret(ILogger logger, string name);
}
