namespace Modules.Scenarios.Application;

using Modules.Scenarios.Domain;
using Modules.Scenarios.Application;

public interface IWeatherIntervalStore
{
    Task AddRange(List<WeatherInterval> weatherIntervals, CancellationToken ct);
    Task<List<WeatherInterval>> GetByScenarioConfigId(Guid scenarioConfigId, CancellationToken ct);
}