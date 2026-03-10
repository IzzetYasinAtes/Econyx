using Econyx.Application.Ports;
using Econyx.Core.Interfaces;
using Econyx.Core.Primitives;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;
using MediatR;

namespace Econyx.Application.Commands.SaveApiKey;

public sealed class SaveApiKeyHandler : IRequestHandler<SaveApiKeyCommand, Result>
{
    private readonly IApiKeyConfigurationRepository _repository;
    private readonly IApiKeyEncryptor _encryptor;
    private readonly IUnitOfWork _unitOfWork;

    public SaveApiKeyHandler(
        IApiKeyConfigurationRepository repository,
        IApiKeyEncryptor encryptor,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _encryptor = encryptor;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SaveApiKeyCommand request, CancellationToken cancellationToken)
    {
        var encrypted = _encryptor.Encrypt(request.ApiKey);
        var masked = _encryptor.Mask(request.ApiKey);

        var existing = await _repository.GetByProviderAsync(request.Provider, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateKey(encrypted, masked);
            _repository.Update(existing);
        }
        else
        {
            var config = ApiKeyConfiguration.Create(request.Provider, encrypted, masked);
            await _repository.AddAsync(config, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
