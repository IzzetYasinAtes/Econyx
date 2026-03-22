namespace Econyx.Application.Queries.GetTradingConfig;

using Econyx.Application.Configuration;
using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Options;

public sealed record GetTradingConfigQuery : IRequest<TradingConfigDto>;

public sealed record TradingConfigDto(
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
    int MaxAiCandidates,
    bool IsFromDatabase);

public sealed class GetTradingConfigHandler : IRequestHandler<GetTradingConfigQuery, TradingConfigDto>
{
    private readonly ITradingConfigurationRepository _repository;
    private readonly TradingOptions _options;

    public GetTradingConfigHandler(
        ITradingConfigurationRepository repository,
        IOptions<TradingOptions> options)
    {
        _repository = repository;
        _options = options.Value;
    }

    public async Task<TradingConfigDto> Handle(GetTradingConfigQuery request, CancellationToken cancellationToken)
    {
        var dbConfig = await _repository.GetActiveAsync(cancellationToken);

        if (dbConfig is not null)
        {
            return new TradingConfigDto(
                dbConfig.Mode, dbConfig.InitialBalance, dbConfig.ScanIntervalMinutes,
                dbConfig.MaxOpenPositions, dbConfig.MaxPositionSizePercent, dbConfig.MinEdgeThreshold,
                dbConfig.MinVolumeUsd, dbConfig.MaxSpreadCents, dbConfig.StopLossPercent,
                dbConfig.TakeProfitPercent, dbConfig.SurvivalModeThresholdUsd, dbConfig.MaxAiCandidates,
                IsFromDatabase: true);
        }

        return new TradingConfigDto(
            _options.Mode, _options.InitialBalance, _options.ScanIntervalMinutes,
            _options.MaxOpenPositions, _options.MaxPositionSizePercent, _options.MinEdgeThreshold,
            _options.MinVolumeUsd, _options.MaxSpreadCents, _options.StopLossPercent,
            _options.TakeProfitPercent, _options.SurvivalModeThresholdUsd, _options.MaxAiCandidates,
            IsFromDatabase: false);
    }
}
