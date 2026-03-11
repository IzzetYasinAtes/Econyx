namespace Econyx.Application.Strategies;

using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;

public interface IStrategy
{
    string Name { get; }
    Task<IReadOnlyList<StrategySignal>> EvaluateAsync(IReadOnlyList<Market> markets, CancellationToken ct = default);
}

public record StrategySignal(
    Guid MarketId,
    string MarketQuestion,
    string TokenId,
    TradeSide RecommendedSide,
    Edge Edge,
    Probability FairValue,
    Probability MarketPrice,
    decimal Confidence,
    string StrategyName,
    string Reasoning);
