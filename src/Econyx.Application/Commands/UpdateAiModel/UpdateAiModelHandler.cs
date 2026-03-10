using Econyx.Core.Interfaces;
using Econyx.Core.Primitives;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;
using MediatR;

namespace Econyx.Application.Commands.UpdateAiModel;

public sealed class UpdateAiModelHandler : IRequestHandler<UpdateAiModelCommand, Result>
{
    private readonly IAiModelConfigurationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAiModelHandler(
        IAiModelConfigurationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateAiModelCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeactivateAllAsync(cancellationToken);

        var candidates = await _repository.FindAsync(
            c => c.ModelId == request.ModelId && c.Provider == request.Provider,
            cancellationToken);
        var existing = candidates.Count > 0 ? candidates[0] : null;

        if (existing is not null)
        {
            existing.UpdateModel(
                request.Provider,
                request.ModelId,
                request.DisplayName,
                request.MaxTokens,
                request.ContextLength,
                request.PromptPricePer1M,
                request.CompletionPricePer1M);
            existing.Activate();
            _repository.Update(existing);
        }
        else
        {
            var config = AiModelConfiguration.Create(
                request.Provider,
                request.ModelId,
                request.DisplayName,
                request.MaxTokens,
                request.ContextLength,
                request.PromptPricePer1M,
                request.CompletionPricePer1M);

            await _repository.AddAsync(config, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
