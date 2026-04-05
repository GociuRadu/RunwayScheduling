using Modules.Airports.Application;
using Modules.Scenarios.Application;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application;

public sealed class ScenarioSnapshotLoader : IScenarioSnapshotLoader
{
    private readonly IScenarioConfigStore _scenarioConfigStore;
    private readonly IAirportStore _airportStore;
    private readonly IFlightStore _flightStore;
    private readonly IWeatherIntervalStore _weatherIntervalStore;
    private readonly IRandomEventStore _randomEventStore;
    private readonly IRunwayStore _runwayStore;

    public ScenarioSnapshotLoader(
        IScenarioConfigStore scenarioConfigStore,
        IAirportStore airportStore,
        IFlightStore flightStore,
        IWeatherIntervalStore weatherIntervalStore,
        IRandomEventStore randomEventStore,
        IRunwayStore runwayStore)
    {
        _scenarioConfigStore = scenarioConfigStore;
        _airportStore = airportStore;
        _flightStore = flightStore;
        _weatherIntervalStore = weatherIntervalStore;
        _randomEventStore = randomEventStore;
        _runwayStore = runwayStore;
    }

    public async Task<ScenarioSnapshot> Load(Guid scenarioConfigId, CancellationToken ct)
    {
        var scenarioConfig = await _scenarioConfigStore.GetById(scenarioConfigId, ct);
        if (scenarioConfig is null)
            throw new Exception("Scenario config not found.");

        var airport = _airportStore.GetAll().FirstOrDefault(x => x.Id == scenarioConfig.AirportId);
        if (airport is null)
            throw new Exception("Airport not found.");

        var flights          = await _flightStore.GetByScenarioConfigId(scenarioConfigId, ct);
        var weatherIntervals = await _weatherIntervalStore.GetByScenarioConfigId(scenarioConfigId, ct);
        var randomEventDtos  = await _randomEventStore.GetAllByScenarioConfigId(scenarioConfigId, ct);
        var runways          = _runwayStore.GetByAirportId(scenarioConfig.AirportId).ToList();

        var randomEvents = randomEventDtos
            .Select(x => new RandomEvent
            {
                ScenarioConfigId = x.ScenarioConfigId,
                Name             = x.Name,
                Description      = x.Description,
                StartTime        = x.StartTime,
                EndTime          = x.EndTime,
                ImpactPercent    = x.ImpactPercent
            })
            .ToList();

        return new ScenarioSnapshot
        {
            ScenarioConfig   = scenarioConfig,
            Airport          = airport,
            Runways          = runways,
            Flights          = flights,
            RandomEvents     = randomEvents,
            WeatherIntervals = weatherIntervals
        };
    }
}
