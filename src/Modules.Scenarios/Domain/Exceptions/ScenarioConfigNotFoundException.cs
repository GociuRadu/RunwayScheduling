namespace Modules.Scenarios.Domain.Exceptions;

public sealed class ScenarioConfigNotFoundException(Guid id)
    : Exception($"Scenario config '{id}' not found.");
