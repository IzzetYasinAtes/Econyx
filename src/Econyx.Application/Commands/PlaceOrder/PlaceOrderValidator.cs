namespace Econyx.Application.Commands.PlaceOrder;

using FluentValidation;

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.Signal)
            .NotNull()
            .WithMessage("Signal is required.");

        RuleFor(x => x.PositionSize)
            .NotNull()
            .WithMessage("Position size is required.");

        RuleFor(x => x.PositionSize.Amount)
            .GreaterThan(0)
            .When(x => x.PositionSize is not null)
            .WithMessage("Position size must be greater than zero.");
    }
}
