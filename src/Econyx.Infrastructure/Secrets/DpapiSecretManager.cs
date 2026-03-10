namespace Econyx.Infrastructure.Secrets;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Econyx.Application.Ports;

internal sealed partial class DpapiSecretManager : ISecretManager
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DpapiSecretManager> _logger;

    public DpapiSecretManager(IConfiguration configuration, ILogger<DpapiSecretManager> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<string> GetSecretAsync(string name, CancellationToken ct = default)
    {
        var value = _configuration[name]
            ?? _configuration[$"Secrets:{name}"]
            ?? throw new KeyNotFoundException($"Secret '{name}' not found in configuration. Ensure it is set via User Secrets or environment variables.");

        LogSecretRetrieved(_logger, name);
        return Task.FromResult(value);
    }

    public Task SetSecretAsync(string name, string value, CancellationToken ct = default)
    {
        LogSetSecretNotSupported(_logger);
        throw new NotSupportedException(
            "DPAPI-backed configuration is read-only at runtime. Use 'dotnet user-secrets set' to manage secrets.");
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Retrieved secret '{Name}' from configuration")]
    private static partial void LogSecretRetrieved(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "SetSecretAsync is not supported by DpapiSecretManager. Use 'dotnet user-secrets set' instead.")]
    private static partial void LogSetSecretNotSupported(ILogger logger);
}
