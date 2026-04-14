using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.Scheduling;

public sealed class PreparedScenario
{
    public ScenarioSnapshot Snapshot { get; init; } = default!;

    public IReadOnlyList<Runway> ActiveRunways { get; init; } = [];
    public IReadOnlyDictionary<FlightType, IReadOnlyList<Runway>> RunwaysByType { get; init; } = new Dictionary<FlightType, IReadOnlyList<Runway>>();

    // Flights sorted by ScheduledTime, then Priority descending — ready for greedy
    public IReadOnlyList<(Flight Flight, Guid SourceId)> SortedFlights { get; init; } = [];

    // Weather intervals sorted by StartTime for efficient lookup
    public IReadOnlyList<WeatherInterval> SortedWeather { get; init; } = [];

    // Random events sorted by StartTime for efficient lookup
    public IReadOnlyList<RandomEvent> SortedEvents { get; init; } = [];

    public static PreparedScenario From(ScenarioSnapshot snapshot)
    {
        var activeRunways = snapshot.Runways
            .Where(r => r.IsActive)
            .ToList();

        var runwaysByType = new Dictionary<FlightType, IReadOnlyList<Runway>>
        {
            [FlightType.Arrival] = activeRunways
                .Where(r => r.RunwayType is RunwayType.Landing or RunwayType.Both)
                .ToList(),
            [FlightType.Departure] = activeRunways
                .Where(r => r.RunwayType is RunwayType.Takeoff or RunwayType.Both)
                .ToList()
        };

        var sortedFlights = snapshot.Flights
            .Select((f, i) => (Flight: f, SourceId: snapshot.FlightSourceIds[i]))
            .OrderBy(x => x.Flight.ScheduledTime)
            .ThenByDescending(x => x.Flight.Priority)
            .ToList();

        var sortedWeather = snapshot.WeatherIntervals
            .OrderBy(w => w.StartTime)
            .ToList();

        var sortedEvents = snapshot.RandomEvents
            .OrderBy(e => e.StartTime)
            .ToList();

        return new PreparedScenario
        {
            Snapshot = snapshot,
            ActiveRunways = activeRunways,
            RunwaysByType = runwaysByType,
            SortedFlights = sortedFlights,
            SortedWeather = sortedWeather,
            SortedEvents = sortedEvents
        };
    }
}
