namespace Modules.Scenarios.Application.UseCases.GetWeatherIntervals;

using MediatR;

public sealed class WeatherIntervalsHandler
    : IRequestHandler<WeatherIntervalsQuery, IReadOnlyList<WeatherIntervalsDto>>
{
    private readonly IWeatherIntervalStore _weatherStore;

    public WeatherIntervalsHandler(IWeatherIntervalStore weatherStore)
        => _weatherStore = weatherStore;

    public async Task<IReadOnlyList<WeatherIntervalsDto>> Handle(
        WeatherIntervalsQuery request,
        CancellationToken ct)
    {
        var entities = await _weatherStore.GetByScenarioConfigId(request.ScenarioConfigId, ct);

        return entities
            .Select(e => new WeatherIntervalsDto(
                e.Id,
                e.ScenarioConfigId,
                e.StartTime,
                e.EndTime,
                e.WeatherType
            ))
            .ToList();
    }
}