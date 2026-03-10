namespace Econyx.Application.Configuration;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string Provider { get; set; } = "OpenRouter";
    public OpenRouterOptions OpenRouter { get; set; } = new();
    public ClaudeOptions Claude { get; set; } = new();
    public OpenAIOptions OpenAI { get; set; } = new();
    public int CacheDurationMinutes { get; set; } = 30;
    public int MaxConcurrentRequests { get; set; } = 3;
}

public sealed class OpenRouterOptions
{
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public string DefaultModel { get; set; } = "anthropic/claude-sonnet-4-20250514";
    public int MaxTokens { get; set; } = 4096;
    public string ApiKeySecretName { get; set; } = "openrouter-api-key";
    public int ModelCacheMinutes { get; set; } = 60;
}

public sealed class ClaudeOptions
{
    public string Model { get; set; } = "claude-sonnet-4-20250514";
    public int MaxTokens { get; set; } = 4096;
    public string ApiKeySecretName { get; set; } = "claude-api-key";
}

public sealed class OpenAIOptions
{
    public string Model { get; set; } = "gpt-4o";
    public int MaxTokens { get; set; } = 4096;
    public string ApiKeySecretName { get; set; } = "openai-api-key";
}
