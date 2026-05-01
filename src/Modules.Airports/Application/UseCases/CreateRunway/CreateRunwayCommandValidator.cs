using FluentValidation;

namespace Modules.Airports.Application.UseCases.CreateRunway;

public sealed class CreateRunwayCommandValidator : AbstractValidator<CreateRunwayCommand>
{
    public CreateRunwayCommandValidator()
    {
        RuleFor(x => x.AirportId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(32);
    }
}
