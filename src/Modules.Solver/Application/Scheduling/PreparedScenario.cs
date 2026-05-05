using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.Scheduling;

public sealed class PreparedScenario
{
    public ScenarioSnapshot Snapshot { get; init; } = default!;

    public IReadOnlyList<Runway> ActiveRunways { get; init; } = [];
    
    // maps each flight type to its compatible runways so the solver doesn't re-filter on every flight
    public IReadOnlyDictionary<FlightType, IReadOnlyList<Runway>> RunwaysByType { get; init; } = new Dictionary<FlightType, IReadOnlyList<Runway>>();
    public IReadOnlyList<(Flight Flight, Guid SourceId)> SortedFlights { get; init; } = [];
    public IReadOnlyList<WeatherInterval> SortedWeather { get; init; } = [];
    public IReadOnlyList<RandomEvent> SortedEvents { get; init; } = [];

    // filters, groups, and sorts the raw snapshot data so the solver can use it directly without extra processing
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
                .ToList(),
            [FlightType.OnGround] = []
        };

        // earliest first; ties broken by priority descending — this order is used directly by greedy and as the seed for GA
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
