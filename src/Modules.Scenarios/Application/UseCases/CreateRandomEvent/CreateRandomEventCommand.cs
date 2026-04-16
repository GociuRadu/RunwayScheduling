using System.ComponentModel.DataAnnotations;
using MediatR;
using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application.UseCases.CreateRandomEvent;

public sealed record CreateRandomEventCommand(
    [property: Required]
    Guid ScenarioConfigId,
    [property: Required]
    [property: StringLength(128, MinimumLength = 2)]
    string Name,
    [property: StringLength(512)]
    string Description,
    DateTime StartTime,
    DateTime EndTime,
    [property: Range(0, 100)]
    int ImpactPercent
) : IRequest<RandomEvent>, IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
        {
            yield return new ValidationResult(
                "End time must be after start time.",
                [nameof(EndTime)]);
        }
    }
}
