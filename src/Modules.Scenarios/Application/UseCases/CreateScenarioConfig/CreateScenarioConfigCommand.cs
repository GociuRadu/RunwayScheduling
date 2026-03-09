using MediatR;
using ScenarioConfigEntity = Modules.Scenarios.Domain.ScenarioConfig;

namespace Modules.Scenarios.Application.UseCases.CreateScenarioConfig;

public sealed record CreateScenarioConfigCommand(
    Guid AirportId,
    string Name,
    int? Difficulty,
    DateTime? StartTime,
    DateTime? EndTime,

    int? Seed,

    int? AircraftCount,
    int? AircraftDifficulty,
    int? OnGroundAircraftCount,
    int? InboundAircraftCount,
    int? RemainingOnGroundAircraftCount,

    int? BaseSeparationSeconds,
    int? WakePercent,
    int? WeatherPercent,

    int? WeatherIntervalCount,
    int? MinWeatherIntervalMinutes,
    int? WeatherDifficulty
) : IRequest<ScenarioConfigEntity>;
