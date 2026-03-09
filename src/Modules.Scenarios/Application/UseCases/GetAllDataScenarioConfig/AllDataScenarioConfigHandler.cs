using MediatR;
using Modules.Scenarios.Application;
using Modules.Scenarios.Domain;
namespace Modules.Scenarios.Application.UseCases.GetAllDataScenarioConfig;

public sealed class GetAllDataScenarioConfigHandler
    : IRequestHandler<GetAllDataScenarioConfigQuery, ScenarioConfigAllDataDto>
{
    private readonly IScenarioConfigStore _configStore;
    private readonly IFlightStore _flightStore;
    private readonly IWeatherIntervalStore _weatherStore;

    public GetAllDataScenarioConfigHandler(
        IScenarioConfigStore configStore,
        IFlightStore flightStore,
        IWeatherIntervalStore weatherStore)
    {
        _configStore = configStore;
        _flightStore = flightStore;
        _weatherStore = weatherStore;
    }

    public async Task<ScenarioConfigAllDataDto> Handle(GetAllDataScenarioConfigQuery request, CancellationToken ct)
    {
        var cfg = await _configStore.GetById(request.ScenarioConfigId, ct);
        if (cfg is null)
            throw new Exception("Scenario config not found");

        var flightsEntities = await _flightStore.GetByScenarioConfigId(cfg.Id, ct);
        var weatherEntities = await _weatherStore.GetByScenarioConfigId(cfg.Id, ct);

        var flights = flightsEntities.Select(f => new FlightDto(
            f.Id,
            f.ScenarioConfigId,
            f.AircraftId,
            f.Callsign,
            f.Type,
            f.ScheduledTime,
            f.MaxDelayMinutes,
            f.MaxEarlyMinutes,
            f.Priority
        )).ToList();

        var intervals = weatherEntities.Select(w => new WeatherIntervalsDto(
            w.Id,
            w.ScenarioConfigId,
            w.StartTime,
            w.EndTime,
            w.WeatherType
        )).ToList();

        return new ScenarioConfigAllDataDto(
            cfg.Id,
            cfg.AirportId,
            cfg.Name,
            cfg.Difficulty,
            cfg.StartTime,
            cfg.EndTime,
            cfg.Seed,
            cfg.AircraftCount,
            cfg.AircraftDifficulty,
            cfg.OnGroundAircraftCount,
            cfg.InboundAircraftCount,
            cfg.RemainingOnGroundAircraftCount,
            cfg.BaseSeparationSeconds,
            cfg.WakePercent,
            cfg.WeatherPercent,
            cfg.WeatherIntervalCount,
            cfg.MinWeatherIntervalMinutes,
            cfg.WeatherDifficulty,
            flights,
            intervals
        );
    }
}