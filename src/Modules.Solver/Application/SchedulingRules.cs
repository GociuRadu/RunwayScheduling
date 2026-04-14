using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application;

internal static class SchedulingRules
{
    public static IReadOnlyList<Runway> GetCompatibleRunways(IEnumerable<Runway> activeRunways, FlightType flightType)
    {
        var requiredType = flightType == FlightType.Arrival ? RunwayType.Landing : RunwayType.Takeoff;

        return activeRunways
            .Where(runway => runway.RunwayType == RunwayType.Both || runway.RunwayType == requiredType)
            .ToList();
    }

    public static WeatherInterval? FindWeatherAt(ScenarioSnapshot snapshot, DateTime instant) =>
        snapshot.WeatherIntervals
            .Where(interval => interval.StartTime <= instant)
            .MaxBy(interval => interval.StartTime);

    public static RandomEvent? FindRandomEventAt(ScenarioSnapshot snapshot, DateTime instant) =>
        snapshot.RandomEvents
            .FirstOrDefault(randomEvent => instant >= randomEvent.StartTime && instant < randomEvent.EndTime);

    public static TimeSpan CalculateSeparation(ScenarioConfig scenarioConfig, WeatherInterval? weather, int? randomEventImpactPercent)
    {
        var baseSeconds = scenarioConfig.BaseSeparationSeconds * (scenarioConfig.WakePercent / 100.0);

        var weatherMultiplier = weather is null
            ? scenarioConfig.WeatherPercent / 100.0
            : weather.WeatherType switch
            {
                WeatherCondition.Clear => 1.00,
                WeatherCondition.Cloud => 1.10,
                WeatherCondition.Rain => 1.30,
                WeatherCondition.Snow => 1.50,
                WeatherCondition.Fog => 1.75,
                WeatherCondition.Storm => 2.00,
                _ => 1.00
            };

        var eventMultiplier = randomEventImpactPercent is > 0 and < 100
            ? 1.0 / (1.0 - (randomEventImpactPercent.Value / 100.0))
            : 1.0;

        return TimeSpan.FromSeconds(baseSeconds * weatherMultiplier * eventMultiplier);
    }

    public static SolvedFlight CreateCanceledFlight(Flight flight, int processingOrder, CancellationReason reason) => new()
    {
        FlightId = flight.Id,
        ScenarioConfigId = flight.ScenarioConfigId,
        AircraftId = flight.AircraftId,
        Callsign = flight.Callsign,
        Type = flight.Type,
        Priority = flight.Priority,
        ProcessingOrder = processingOrder,
        ScheduledTime = flight.ScheduledTime,
        MaxDelayMinutes = flight.MaxDelayMinutes,
        MaxEarlyMinutes = flight.MaxEarlyMinutes,
        Status = FlightStatus.Canceled,
        CancellationReason = reason,
        AssignedRunway = null,
        AssignedTime = null,
        DelayMinutes = 0,
        EarlyMinutes = 0,
        SeparationAppliedSeconds = 0,
        WeatherAtAssignment = null,
        AffectedByRandomEvent = false
    };
}
