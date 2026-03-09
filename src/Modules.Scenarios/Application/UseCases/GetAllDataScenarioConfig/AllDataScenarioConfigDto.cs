using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application.UseCases.GetAllDataScenarioConfig;

public sealed record ScenarioConfigAllDataDto(
    Guid Id,
    Guid AirportId,
    string Name,
    int Difficulty,
    DateTime StartTime,
    DateTime EndTime,
    int Seed,
    int AircraftCount,
    int AircraftDifficulty,
    int OnGroundAircraftCount,
    int InboundAircraftCount,
    int RemainingOnGroundAircraftCount,
    int BaseSeparationSeconds,
    int WakePercent,
    int WeatherPercent,
    int WeatherIntervalCount,
    int MinWeatherIntervalMinutes,
    int WeatherDifficulty,
    IReadOnlyList<FlightDto> Flights,
    IReadOnlyList<WeatherIntervalsDto> WeatherIntervals
);

public sealed record FlightDto(
    Guid Id,
    Guid ScenarioConfigId,
    Guid AircraftId,
    string Callsign,
    FlightType Type,
    DateTime ScheduledTime,
    int MaxDelayMinutes,
    int MaxEarlyMinutes,
    int Priority

);

public sealed record WeatherIntervalsDto(
    Guid Id,
    Guid ScenarioConfigId,
    DateTime StartTime,
    DateTime EndTime,
    WeatherCondition WeatherType
);