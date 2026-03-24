using MediatR;

namespace Modules.Scenarios.Application.UseCases.GetRandomEventsByScenarioConfigId;

public sealed record GetRandomEventsByScenarioConfigIdQuery(Guid ScenarioConfigId)
    : IRequest<IReadOnlyList<RandomEventDto>>;