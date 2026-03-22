namespace Econyx.Application.Commands.SaveTradingConfig;

using Econyx.Core.Primitives;
using Econyx.Domain.Enums;
using MediatR;

public sealed record SaveTradingConfigCommand(
    TradingMode Mode,
    decimal InitialBalance,
    int ScanIntervalMinutes,
    int MaxOpenPositions,
    decimal MaxPositionSizePercent,
    decimal MinEdgeThreshold,
    decimal MinVolumeUsd,
    decimal MaxSpreadCents,
    decimal StopLossPercent,
    decimal TakeProfitPercent,
    decimal SurvivalModeThresholdUsd,
    int MaxAiCandidates) : IRequest<Result<Guid>>;
