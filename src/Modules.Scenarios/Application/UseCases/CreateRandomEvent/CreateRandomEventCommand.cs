using MediatR;
using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application.UseCases.CreateRandomEvent;

public sealed record CreateRandomEventCommand(
    Guid ScenarioConfigId,
    string Name,
    string Description,
    DateTime StartTime,
    DateTime EndTime,
    int ImpactPercent
) : IRequest<RandomEvent>;