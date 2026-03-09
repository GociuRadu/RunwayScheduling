namespace Modules.Scenarios.Application.UseCases.GetScenarioConfigs;

public sealed record ScenarioConfigDto(
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
    int WeatherDifficulty
);
