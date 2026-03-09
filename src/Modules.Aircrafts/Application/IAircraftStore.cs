namespace Modules.Aircrafts.Application;

using Modules.Aircrafts.Domain;

public interface IAircraftStore
{
    Task<List<Aircraft>> GetByScenarioId(Guid scenarioConfigId, CancellationToken ct);
    Task AddRange(List<Aircraft> aircrafts, CancellationToken ct);
}