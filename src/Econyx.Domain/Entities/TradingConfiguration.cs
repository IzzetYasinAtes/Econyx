using Econyx.Core.Entities;
using Econyx.Domain.Enums;

namespace Econyx.Domain.Entities;

public sealed class TradingConfiguration : BaseEntity<Guid>
{
    public TradingMode Mode { get; private set; }
    public decimal InitialBalance { get; private set; }
    public int ScanIntervalMinutes { get; private set; }
    public int MaxOpenPositions { get; private set; }
    public decimal MaxPositionSizePercent { get; private set; }
    public decimal MinEdgeThreshold { get; private set; }
    public decimal MinVolumeUsd { get; private set; }
    public decimal MaxSpreadCents { get; private set; }
    public decimal StopLossPercent { get; private set; }
    public decimal TakeProfitPercent { get; private set; }
    public decimal SurvivalModeThresholdUsd { get; private set; }
    public int MaxAiCandidates { get; private set; }
    public bool IsActive { get; private set; }

    private TradingConfiguration() { }

    public static TradingConfiguration Create(
        TradingMode mode,
        decimal initialBalance,
        int scanIntervalMinutes,
        int maxOpenPositions,
        decimal maxPositionSizePercent,
        decimal minEdgeThreshold,
        decimal minVolumeUsd,
        decimal maxSpreadCents,
        decimal stopLossPercent,
        decimal takeProfitPercent,
        decimal survivalModeThresholdUsd,
        int maxAiCandidates)
    {
        return new TradingConfiguration
        {
            Id = Guid.NewGuid(),
            Mode = mode,
            InitialBalance = initialBalance,
            ScanIntervalMinutes = scanIntervalMinutes,
            MaxOpenPositions = maxOpenPositions,
            MaxPositionSizePercent = maxPositionSizePercent,
            MinEdgeThreshold = minEdgeThreshold,
            MinVolumeUsd = minVolumeUsd,
            MaxSpreadCents = maxSpreadCents,
            StopLossPercent = stopLossPercent,
            TakeProfitPercent = takeProfitPercent,
            SurvivalModeThresholdUsd = survivalModeThresholdUsd,
            MaxAiCandidates = maxAiCandidates,
            IsActive = true
        };
    }

    public void Update(
        TradingMode mode,
        decimal initialBalance,
        int scanIntervalMinutes,
        int maxOpenPositions,
        decimal maxPositionSizePercent,
        decimal minEdgeThreshold,
        decimal minVolumeUsd,
        decimal maxSpreadCents,
        decimal stopLossPercent,
        decimal takeProfitPercent,
        decimal survivalModeThresholdUsd,
        int maxAiCandidates)
    {
        Mode = mode;
        InitialBalance = initialBalance;
        ScanIntervalMinutes = scanIntervalMinutes;
        MaxOpenPositions = maxOpenPositions;
        MaxPositionSizePercent = maxPositionSizePercent;
        MinEdgeThreshold = minEdgeThreshold;
        MinVolumeUsd = minVolumeUsd;
        MaxSpreadCents = maxSpreadCents;
        StopLossPercent = stopLossPercent;
        TakeProfitPercent = takeProfitPercent;
        SurvivalModeThresholdUsd = survivalModeThresholdUsd;
        MaxAiCandidates = maxAiCandidates;
        UpdatedAt = DateTime.UtcNow;
    }
}
