using MediatR;
using Modules.Airports.Application;
using Modules.Airports.Application.UseCases.GetRunwaysByAirportId;
using Modules.Airports.Domain;
using Modules.Scenarios.Application;
using Modules.Scenarios.Application.UseCases.GetFlights;
using Modules.Scenarios.Application.UseCases.GetRandomEventsByScenarioConfigId;
using Modules.Scenarios.Application.UseCases.GetWeatherIntervals;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.Snapshot;

public sealed class ScenarioSnapshotFactory : IScenarioSnapshotFactory
{
    private readonly ISender _sender;
    private readonly IScenarioConfigStore _scenarioConfigStore;
    private readonly IAirportStore _airportStore;

    public ScenarioSnapshotFactory(
        ISender sender,
        IScenarioConfigStore scenarioConfigStore,
        IAirportStore airportStore)
    {
        _sender = sender;
        _scenarioConfigStore = scenarioConfigStore;
        _airportStore = airportStore;
    }

    public async Task<ScenarioSnapshot> CreateAsync(Guid scenarioConfigId, CancellationToken ct)
    {
        var scenarioConfig = await _scenarioConfigStore.GetById(scenarioConfigId, ct)
            ?? throw new InvalidOperationException($"ScenarioConfig {scenarioConfigId} not found.");

        var airport = _airportStore.GetAll().FirstOrDefault(x => x.Id == scenarioConfig.AirportId)
            ?? throw new InvalidOperationException($"Airport {scenarioConfig.AirportId} not found.");

        var flightDtos = await _sender.Send(new FlightQuery(scenarioConfigId), ct);
        var weatherDtos = await _sender.Send(new WeatherIntervalsQuery(scenarioConfigId), ct);
        var randomEventDtos = await _sender.Send(new GetRandomEventsByScenarioConfigIdQuery(scenarioConfigId), ct);
        var runwayDtos = await _sender.Send(new GetRunwaysByAirportIdQuery(scenarioConfig.AirportId), ct);

        var flights = flightDtos.Select(x => new Flight
        {
            ScenarioConfigId = x.ScenarioConfigId,
            AircraftId = x.AircraftId,
            Callsign = x.Callsign,
            Type = x.Type,
            ScheduledTime = x.ScheduledTime,
            MaxDelayMinutes = x.MaxDelayMinutes,
            MaxEarlyMinutes = x.MaxEarlyMinutes,
            Priority = x.Priority
        }).ToList();

        var weatherIntervals = weatherDtos.Select(x => new WeatherInterval
        {
            ScenarioConfigId = x.ScenarioConfigId,
            StartTime = x.StartTime,
            EndTime = x.EndTime,
            WeatherType = x.Condition
        }).ToList();

        var randomEvents = randomEventDtos.Select(x => new RandomEvent
        {
            ScenarioConfigId = x.ScenarioConfigId,
            Name = x.Name,
            Description = x.Description,
            StartTime = x.StartTime,
            EndTime = x.EndTime,
            ImpactPercent = x.ImpactPercent
        }).ToList();

        var runways = runwayDtos.Select(x => new Runway
        {
            AirportId = x.AirportId,
            Name = x.Name,
            IsActive = x.IsActive,
            RunwayType = x.RunwayType
        }).ToList();

        return new ScenarioSnapshot
        {
            ScenarioConfig = scenarioConfig,
            Airport = airport,
            Runways = runways,
            RunwaySourceIds = runwayDtos.Select(x => x.id).ToList(),
            Flights = flights,
            FlightSourceIds = flightDtos.Select(x => x.Id).ToList(),
            RandomEvents = randomEvents,
            WeatherIntervals = weatherIntervals
        };
    }
}
