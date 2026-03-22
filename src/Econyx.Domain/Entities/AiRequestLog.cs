using Econyx.Core.Entities;

namespace Econyx.Domain.Entities;

public sealed class AiRequestLog : BaseEntity<Guid>
{
    public string Provider { get; private set; } = null!;
    public string ModelId { get; private set; } = null!;
    public string MarketQuestion { get; private set; } = null!;
    public string Prompt { get; private set; } = null!;
    public string? Response { get; private set; }
    public string? ParsedReasoning { get; private set; }
    public decimal? FairValue { get; private set; }
    public decimal? Confidence { get; private set; }
    public int InputTokens { get; private set; }
    public int OutputTokens { get; private set; }
    public decimal CostUsd { get; private set; }
    public bool IsSuccess { get; private set; }
    public bool IsCacheHit { get; private set; }
    public string? ErrorMessage { get; private set; }

    private AiRequestLog() { }

    public static AiRequestLog Create(
        string provider,
        string modelId,
        string marketQuestion,
        string prompt,
        string? response,
        string? parsedReasoning,
        decimal? fairValue,
        decimal? confidence,
        int inputTokens,
        int outputTokens,
        decimal costUsd,
        bool isSuccess,
        bool isCacheHit,
        string? errorMessage)
    {
        return new AiRequestLog
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            ModelId = modelId,
            MarketQuestion = marketQuestion,
            Prompt = prompt,
            Response = response,
            ParsedReasoning = parsedReasoning,
            FairValue = fairValue,
            Confidence = confidence,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            CostUsd = costUsd,
            IsSuccess = isSuccess,
            IsCacheHit = isCacheHit,
            ErrorMessage = errorMessage
        };
    }
}
