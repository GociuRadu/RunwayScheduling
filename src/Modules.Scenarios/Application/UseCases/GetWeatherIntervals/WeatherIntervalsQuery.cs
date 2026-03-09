namespace Modules.Scenarios.Application.UseCases.GetWeatherIntervals;

using MediatR;

public sealed record WeatherIntervalsQuery(Guid ScenarioConfigId)
    : IRequest<IReadOnlyList<WeatherIntervalsDto>>;