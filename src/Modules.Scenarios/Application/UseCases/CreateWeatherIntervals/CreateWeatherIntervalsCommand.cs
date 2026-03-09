using MediatR;
using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application.UseCases.CreateWeatherIntervals;

public sealed record CreateWeatherIntervalsCommand(Guid ScenarioConfigId) : IRequest<IReadOnlyList<WeatherInterval>>;