namespace Econyx.Application.Queries.GetMarketAnalysis;

using Econyx.Application.Ports;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using MediatR;

public sealed record GetMarketAnalysisQuery(Guid MarketId) : IRequest<MarketAnalysisDto?>;

public sealed record MarketAnalysisDto(
    Guid MarketId,
    string Question,
    string Description,
    string Category,
    PlatformType Platform,
    MarketStatus Status,
    decimal VolumeUsd,
    decimal Spread,
    IReadOnlyList<OutcomeDto> Outcomes,
    FairValueResult? AiAnalysis);

public sealed record OutcomeDto(string Name, decimal Price, string TokenId);
