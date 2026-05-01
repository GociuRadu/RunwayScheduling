using FluentValidation;

namespace Modules.Scenarios.Application.UseCases.CreateScenarioConfig;

public sealed class CreateScenarioConfigCommandValidator : AbstractValidator<CreateScenarioConfigCommand>
{
    public CreateScenarioConfigCommandValidator()
    {
        RuleFor(x => x.AirportId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(128);
        RuleFor(x => x.Difficulty).InclusiveBetween(1, 5).When(x => x.Difficulty.HasValue);
        RuleFor(x => x.StartTime).NotNull();
        RuleFor(x => x.EndTime).NotNull()
            .GreaterThan(x => x.StartTime!.Value)
            .When(x => x.StartTime.HasValue && x.EndTime.HasValue);
        RuleFor(x => x).Must(x =>
                x.AircraftCount is null || x.OnGroundAircraftCount is null || x.InboundAircraftCount is null ||
                x.AircraftCount == x.OnGroundAircraftCount + x.InboundAircraftCount)
            .WithMessage("Aircraft count must equal on ground plus inbound aircraft.");
        RuleFor(x => x.RemainingOnGroundAircraftCount)
            .LessThanOrEqualTo(x => x.OnGroundAircraftCount!.Value)
            .When(x => x.RemainingOnGroundAircraftCount.HasValue && x.OnGroundAircraftCount.HasValue);
    }
}
