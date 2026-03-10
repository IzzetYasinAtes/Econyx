using Econyx.Core.Entities;
using Econyx.Domain.Enums;

namespace Econyx.Domain.Entities;

public sealed class ApiKeyConfiguration : BaseEntity<Guid>
{
    public AiProviderType Provider { get; private set; }
    public string EncryptedKey { get; private set; } = null!;
    public bool IsConfigured { get; private set; }
    public string? MaskedDisplay { get; private set; }

    private ApiKeyConfiguration() { }

    public static ApiKeyConfiguration Create(AiProviderType provider, string encryptedKey, string maskedDisplay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedKey);

        return new ApiKeyConfiguration
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            EncryptedKey = encryptedKey,
            IsConfigured = true,
            MaskedDisplay = maskedDisplay
        };
    }

    public void UpdateKey(string encryptedKey, string maskedDisplay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedKey);
        EncryptedKey = encryptedKey;
        MaskedDisplay = maskedDisplay;
        IsConfigured = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
