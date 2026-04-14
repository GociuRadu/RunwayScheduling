using Modules.Solver.Domain;

namespace Modules.Solver.Application.Snapshot;

public interface IScenarioSnapshotFactory
{
    Task<ScenarioSnapshot> CreateAsync(Guid scenarioConfigId, CancellationToken ct);
}
