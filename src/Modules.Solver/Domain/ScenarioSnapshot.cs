using Modules.Scenarios.Domain;
using Modules.Airports.Domain;
using Modules.Aircrafts.Domain;

namespace Modules.Solver.Domain;

public sealed class ScenarioSnapshot
{
    public ScenarioConfig ScenarioConfig { get; init; } = default!;
    public Airport Airport { get; init; } = default!;
    public IReadOnlyList<Runway> Runways { get; init; } = [];
    public IReadOnlyList<Flight> Flights { get; init; } = [];
    public IReadOnlyList<RandomEvent> RandomEvents { get; init; } = [];
    public IReadOnlyList<WeatherInterval> WeatherIntervals { get; init; } = [];
}