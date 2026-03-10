namespace Econyx.Application.Ports;

public interface IOpenRouterClient
{
    Task<IReadOnlyList<OpenRouterModelDto>> GetAvailableModelsAsync(CancellationToken ct = default);
}

public record OpenRouterModelDto(
    string Id,
    string Name,
    string? Description,
    int ContextLength,
    decimal PromptPricePer1M,
    decimal CompletionPricePer1M);
