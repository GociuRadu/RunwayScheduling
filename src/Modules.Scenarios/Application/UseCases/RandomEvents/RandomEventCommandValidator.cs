using System.ComponentModel.DataAnnotations;
using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application.UseCases.RandomEvents;

internal static class RandomEventCommandValidator
{
    public static void Validate(
        ScenarioConfig? scenarioConfig,
        string name,
        DateTime startTime,
        DateTime endTime,
        int impactPercent)
    {
        if (scenarioConfig is null)
        {
            throw new KeyNotFoundException("Scenario config was not found.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Random event name is required.");
        }

        if (endTime <= startTime)
        {
            throw new ValidationException("Random event end time must be after the start time.");
        }

        if (impactPercent is < 0 or > 100)
        {
            throw new ValidationException("Impact percent must be between 0 and 100.");
        }

        if (startTime < scenarioConfig.StartTime || endTime > scenarioConfig.EndTime)
        {
            throw new ValidationException("Random events must stay within the scenario window.");
        }
    }
}
