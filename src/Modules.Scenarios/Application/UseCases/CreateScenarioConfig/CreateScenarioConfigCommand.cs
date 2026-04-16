using System.ComponentModel.DataAnnotations;
using MediatR;
using ScenarioConfigEntity = Modules.Scenarios.Domain.ScenarioConfig;

namespace Modules.Scenarios.Application.UseCases.CreateScenarioConfig;

public sealed record CreateScenarioConfigCommand(
    [property: Required]
    Guid AirportId,
    [property: Required]
    [property: StringLength(128, MinimumLength = 2)]
    string Name,
    [property: Range(1, 5)]
    int? Difficulty,
    DateTime? StartTime,
    DateTime? EndTime,

    [property: Range(0, int.MaxValue)]
    int? Seed,

    [property: Range(1, int.MaxValue)]
    int? AircraftCount,
    [property: Range(1, 5)]
    int? AircraftDifficulty,
    [property: Range(0, int.MaxValue)]
    int? OnGroundAircraftCount,
    [property: Range(0, int.MaxValue)]
    int? InboundAircraftCount,
    [property: Range(0, int.MaxValue)]
    int? RemainingOnGroundAircraftCount,

    [property: Range(1, int.MaxValue)]
    int? BaseSeparationSeconds,
    [property: Range(0, 200)]
    int? WakePercent,
    [property: Range(0, 200)]
    int? WeatherPercent,

    [property: Range(0, int.MaxValue)]
    int? WeatherIntervalCount,
    [property: Range(1, int.MaxValue)]
    int? MinWeatherIntervalMinutes,
    [property: Range(1, 5)]
    int? WeatherDifficulty
) : IRequest<ScenarioConfigEntity>, IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime is null)
        {
            yield return new ValidationResult("Start time is required.", [nameof(StartTime)]);
        }

        if (EndTime is null)
        {
            yield return new ValidationResult("End time is required.", [nameof(EndTime)]);
        }

        if (StartTime is not null && EndTime is not null && EndTime <= StartTime)
        {
            yield return new ValidationResult("End time must be after start time.", [nameof(EndTime)]);
        }

        if (OnGroundAircraftCount is not null
            && RemainingOnGroundAircraftCount is not null
            && RemainingOnGroundAircraftCount > OnGroundAircraftCount)
        {
            yield return new ValidationResult(
                "Remaining on ground aircraft cannot exceed on ground aircraft.",
                [nameof(RemainingOnGroundAircraftCount)]);
        }

        if (AircraftCount is not null
            && OnGroundAircraftCount is not null
            && InboundAircraftCount is not null
            && AircraftCount != OnGroundAircraftCount + InboundAircraftCount)
        {
            yield return new ValidationResult(
                "Aircraft count must equal on ground plus inbound aircraft.",
                [nameof(AircraftCount)]);
        }
    }
}
