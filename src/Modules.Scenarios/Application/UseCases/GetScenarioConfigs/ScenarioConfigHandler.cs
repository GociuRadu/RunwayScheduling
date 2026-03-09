using MediatR;

namespace Modules.Scenarios.Application.UseCases.GetScenarioConfigs;

public sealed class ScenarioConfigHandler : IRequestHandler<ScenarioConfigQuery, List<ScenarioConfigDto>>
{
    private readonly IScenarioConfigStore _store;

    public ScenarioConfigHandler(IScenarioConfigStore store) => _store = store;

    public async Task<List<ScenarioConfigDto>> Handle(ScenarioConfigQuery request, CancellationToken ct)
    {
        var entities = await _store.GetAll(ct);

        return entities.Select(e => new ScenarioConfigDto(
            e.Id,
            e.AirportId,
            e.Name,
            e.Difficulty,
            e.StartTime,
            e.EndTime,
            e.Seed,
            e.AircraftCount,
            e.AircraftDifficulty,
            e.OnGroundAircraftCount,
            e.InboundAircraftCount,
            e.RemainingOnGroundAircraftCount,
            e.BaseSeparationSeconds,
            e.WakePercent,
            e.WeatherPercent,
            e.WeatherIntervalCount,
            e.MinWeatherIntervalMinutes,
            e.WeatherDifficulty
        )).ToList();
    }
}
