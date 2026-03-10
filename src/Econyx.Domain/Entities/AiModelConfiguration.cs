using Econyx.Core.Entities;
using Econyx.Domain.Enums;

namespace Econyx.Domain.Entities;

public sealed class AiModelConfiguration : BaseEntity<Guid>
{
    public AiProviderType Provider { get; private set; }
    public string ModelId { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public int MaxTokens { get; private set; }
    public int ContextLength { get; private set; }
    public decimal PromptPricePer1M { get; private set; }
    public decimal CompletionPricePer1M { get; private set; }
    public bool IsActive { get; private set; }

    private AiModelConfiguration() { }

    public static AiModelConfiguration Create(
        AiProviderType provider,
        string modelId,
        string displayName,
        int maxTokens = 4096,
        int contextLength = 128_000,
        decimal promptPricePer1M = 0m,
        decimal completionPricePer1M = 0m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new AiModelConfiguration
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            ModelId = modelId,
            DisplayName = displayName,
            MaxTokens = maxTokens,
            ContextLength = contextLength,
            PromptPricePer1M = promptPricePer1M,
            CompletionPricePer1M = completionPricePer1M,
            IsActive = true
        };
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateModel(
        AiProviderType provider,
        string modelId,
        string displayName,
        int maxTokens,
        int contextLength,
        decimal promptPricePer1M,
        decimal completionPricePer1M)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Provider = provider;
        ModelId = modelId;
        DisplayName = displayName;
        MaxTokens = maxTokens;
        ContextLength = contextLength;
        PromptPricePer1M = promptPricePer1M;
        CompletionPricePer1M = completionPricePer1M;
        UpdatedAt = DateTime.UtcNow;
    }
}
