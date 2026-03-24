using Modules.Solver.Domain;

namespace Modules.Solver.Application;

public interface IScenarioSnapshotLoader
{
    Task<ScenarioSnapshot> Load(Guid scenarioConfigId, CancellationToken ct);
}