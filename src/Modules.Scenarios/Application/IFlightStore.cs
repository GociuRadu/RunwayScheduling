using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application;

public interface IFlightStore
{
    Task AddRange(List<Flight> flights, CancellationToken ct);
    Task SaveChanges(CancellationToken ct);
    Task<List<Flight>> GetByScenarioConfigId(Guid scenarioConfigId, CancellationToken ct);
}