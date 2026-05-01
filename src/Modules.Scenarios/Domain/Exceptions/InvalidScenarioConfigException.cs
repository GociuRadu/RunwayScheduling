namespace Modules.Scenarios.Domain.Exceptions;

public sealed class InvalidScenarioConfigException(string message)
    : Exception(message);
