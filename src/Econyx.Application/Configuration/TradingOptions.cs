namespace Econyx.Application.Configuration;

using Econyx.Domain.Enums;

public sealed class TradingOptions
{
    public const string SectionName = "Trading";

    public TradingMode Mode { get; set; } = TradingMode.Paper;
    public decimal InitialBalance { get; set; } = 50m;
    public int ScanIntervalMinutes { get; set; } = 1;
    public int MaxOpenPositions { get; set; } = 20;
    public decimal MaxPositionSizePercent { get; set; } = 2m;
    public decimal MinEdgeThreshold { get; set; } = 0.05m;
    public decimal MinVolumeUsd { get; set; } = 50_000m;
    public decimal MaxSpreadCents { get; set; } = 5m;
    public decimal StopLossPercent { get; set; } = 10m;
    public decimal TakeProfitPercent { get; set; } = 15m;
    public int MaxHoldMinutes { get; set; } = 15;
    public decimal SurvivalModeThresholdUsd { get; set; } = 10m;
    public int MaxAiCandidates { get; set; } = 20;
}
