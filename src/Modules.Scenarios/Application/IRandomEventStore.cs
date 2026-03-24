using Modules.Scenarios.Domain;
using Modules.Scenarios.Application.UseCases.GetRandomEventsByScenarioConfigId;

namespace Modules.Scenarios.Application;

public interface IRandomEventStore
{
    Task<RandomEvent> Add(RandomEvent randomEvent, CancellationToken ct);
    Task<bool> Delete(Guid id, CancellationToken ct);
    Task<IReadOnlyList<RandomEventDto>> GetAllByScenarioConfigId(Guid scenarioConfigId, CancellationToken ct);
    Task<RandomEvent?> Update(
    Guid id, Guid scenarioConfigId,
     string name, string description, DateTime startTime, DateTime endTime, int impactPercent, CancellationToken ct
);
}