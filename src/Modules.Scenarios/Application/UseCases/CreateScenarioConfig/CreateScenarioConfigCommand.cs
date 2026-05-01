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
) : IRequest<ScenarioConfigEntity>;
