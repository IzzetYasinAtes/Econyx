namespace Econyx.Application.Commands.SaveTradingConfig;

using Econyx.Core.Interfaces;
using Econyx.Core.Primitives;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;
using MediatR;

public sealed class SaveTradingConfigHandler : IRequestHandler<SaveTradingConfigCommand, Result<Guid>>
{
    private readonly ITradingConfigurationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveTradingConfigHandler(
        ITradingConfigurationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(SaveTradingConfigCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetActiveAsync(cancellationToken);

        if (existing is not null)
        {
            existing.Update(
                request.Mode, request.InitialBalance, request.ScanIntervalMinutes,
                request.MaxOpenPositions, request.MaxPositionSizePercent, request.MinEdgeThreshold,
                request.MinVolumeUsd, request.MaxSpreadCents, request.StopLossPercent,
                request.TakeProfitPercent, request.SurvivalModeThresholdUsd, request.MaxAiCandidates);

            _repository.Update(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(existing.Id);
        }

        var config = TradingConfiguration.Create(
            request.Mode, request.InitialBalance, request.ScanIntervalMinutes,
            request.MaxOpenPositions, request.MaxPositionSizePercent, request.MinEdgeThreshold,
            request.MinVolumeUsd, request.MaxSpreadCents, request.StopLossPercent,
            request.TakeProfitPercent, request.SurvivalModeThresholdUsd, request.MaxAiCandidates);

        await _repository.AddAsync(config, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(config.Id);
    }
}
