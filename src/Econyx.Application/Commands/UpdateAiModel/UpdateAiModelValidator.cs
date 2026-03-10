using FluentValidation;

namespace Econyx.Application.Commands.UpdateAiModel;

public sealed class UpdateAiModelValidator : AbstractValidator<UpdateAiModelCommand>
{
    public UpdateAiModelValidator()
    {
        RuleFor(x => x.ModelId).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty();
        RuleFor(x => x.MaxTokens).GreaterThan(0);
        RuleFor(x => x.ContextLength).GreaterThan(0);
        RuleFor(x => x.Provider).IsInEnum();
    }
}
