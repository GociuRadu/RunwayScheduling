using System.ComponentModel.DataAnnotations;
using MediatR;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.UseCases.SolveFromPayload;

public sealed record SolveFromPayloadQuery(
    [property: Required]
    [property: RegularExpression("^(greedy|genetic)$", ErrorMessage = "Algorithm must be either 'greedy' or 'genetic'.")]
    string Algorithm,
    [property: Required]
    ScenarioPayload ScenarioConfig,
    [property: MinLength(1)]
    IReadOnlyList<RunwayPayload> Runways,
    [property: MinLength(1)]
    IReadOnlyList<FlightPayload> Flights,
    [property: Required]
    IReadOnlyList<WeatherIntervalPayload> WeatherIntervals,
    [property: Required]
    IReadOnlyList<RandomEventPayload> RandomEvents
) : IRequest<SolverResult>;

public sealed record ScenarioPayload(
    [property: Required]
    [property: StringLength(128, MinimumLength = 2)]
    string Name,
    DateTime StartTime,
    DateTime EndTime,
    [property: Range(1, int.MaxValue)]
    int BaseSeparationSeconds
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult("End time must be after start time.", [nameof(EndTime)]);
        }
    }
}

public sealed record RunwayPayload(
    [property: Required]
    [property: StringLength(32, MinimumLength = 2)]
    string Name,
    [property: Range(0, 2)]
    int RunwayType,
    bool IsActive
);

public sealed record FlightPayload(
    [property: Required]
    [property: StringLength(32, MinimumLength = 2)]
    string Callsign,
    [property: Range(0, 2)]
    int Type,
    DateTime ScheduledTime,
    [property: Range(0, int.MaxValue)]
    int MaxDelayMinutes,
    [property: Range(0, int.MaxValue)]
    int MaxEarlyMinutes,
    [property: Range(1, 10)]
    int Priority
);

public sealed record WeatherIntervalPayload(
    DateTime StartTime,
    DateTime EndTime,
    [property: Range(0, 5)]
    int WeatherType
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult("End time must be after start time.", [nameof(EndTime)]);
        }
    }
}

public sealed record RandomEventPayload(
    [property: Required]
    [property: StringLength(128, MinimumLength = 2)]
    string Name,
    [property: StringLength(512)]
    string Description,
    DateTime StartTime,
    DateTime EndTime,
    [property: Range(0, 100)]
    int ImpactPercent
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult("End time must be after start time.", [nameof(EndTime)]);
        }
    }
}
