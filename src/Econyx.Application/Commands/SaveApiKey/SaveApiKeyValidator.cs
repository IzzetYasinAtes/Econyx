using FluentValidation;

namespace Econyx.Application.Commands.SaveApiKey;

public sealed class SaveApiKeyValidator : AbstractValidator<SaveApiKeyCommand>
{
    public SaveApiKeyValidator()
    {
        RuleFor(x => x.Provider).IsInEnum();
        RuleFor(x => x.ApiKey).NotEmpty().MinimumLength(10);
    }
}
