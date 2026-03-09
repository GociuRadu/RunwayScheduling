using ScenarioConfigEntity = Modules.Scenarios.Domain.ScenarioConfig;

namespace Modules.Scenarios.Application;

public interface IScenarioConfigStore
{
    Task<ScenarioConfigEntity> Add(ScenarioConfigEntity config, CancellationToken ct);
    Task<List<ScenarioConfigEntity>> GetAll(CancellationToken ct);
    Task<ScenarioConfigEntity?> GetById(Guid id, CancellationToken ct);
    Task<bool> Delete(Guid id, CancellationToken ct);
}
