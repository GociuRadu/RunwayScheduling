using MediatR;

namespace Modules.Scenarios.Application.UseCases.GetAllDataScenarioConfig;

public sealed record GetAllDataScenarioConfigQuery(Guid ScenarioConfigId)
    : IRequest<ScenarioConfigAllDataDto>;