using MediatR;
using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application.UseCases.UpdateRandomEvent;

public sealed record UpdateRandomEventCommand(
    Guid Id,
    Guid ScenarioConfigId,
    string Name,
    string Description,
    DateTime StartTime,
    DateTime EndTime,
    int ImpactPercent
) : IRequest<RandomEvent>;