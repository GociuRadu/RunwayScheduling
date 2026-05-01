using FluentValidation;

namespace Modules.Airports.Application.UseCases.CreateAirport;

public sealed class CreateAirportCommandValidator : AbstractValidator<CreateAirportCommand>
{
    public CreateAirportCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(128);
        RuleFor(x => x.StandCapacity).InclusiveBetween(0, 1000);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}
