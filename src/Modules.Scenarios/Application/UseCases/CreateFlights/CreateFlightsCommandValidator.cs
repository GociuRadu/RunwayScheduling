using FluentValidation;

namespace Modules.Scenarios.Application.UseCases.CreateFlights;

public sealed class CreateFlightsCommandValidator : AbstractValidator<CreateFlightsCommand>
{
    public CreateFlightsCommandValidator()
    {
        RuleFor(x => x.ScenarioConfigId).NotEmpty();
    }
}
