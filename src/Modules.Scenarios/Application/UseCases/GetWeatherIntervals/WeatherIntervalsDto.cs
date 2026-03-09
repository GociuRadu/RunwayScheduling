namespace Modules.Scenarios.Application.UseCases.GetWeatherIntervals;

using Modules.Scenarios.Domain;

public sealed record WeatherIntervalsDto(
    Guid Id,
    Guid ScenarioConfigId,
    DateTime StartTime,
    DateTime EndTime,
    WeatherCondition Condition
);