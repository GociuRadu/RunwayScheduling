namespace Modules.Scenarios.Application.UseCases.GetRandomEventsByScenarioConfigId;

public sealed record RandomEventDto(
    Guid Id,
    Guid ScenarioConfigId,
    string Name,
    string Description,
    DateTime StartTime,
    DateTime EndTime,
    int ImpactPercent
);