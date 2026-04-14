using Modules.Airports.Domain;
using Modules.Scenarios.Domain;

namespace Modules.Solver.Domain;

public sealed class ScenarioSnapshot
{
    public ScenarioConfig ScenarioConfig { get; init; } = default!;
    public Airport Airport { get; init; } = default!;
    public IReadOnlyList<Runway> Runways { get; init; } = [];
    public IReadOnlyList<Guid> RunwaySourceIds { get; init; } = [];
    public IReadOnlyList<Flight> Flights { get; init; } = [];
    public IReadOnlyList<Guid> FlightSourceIds { get; init; } = [];
    public IReadOnlyList<RandomEvent> RandomEvents { get; init; } = [];
    public IReadOnlyList<WeatherInterval> WeatherIntervals { get; init; } = [];
}
