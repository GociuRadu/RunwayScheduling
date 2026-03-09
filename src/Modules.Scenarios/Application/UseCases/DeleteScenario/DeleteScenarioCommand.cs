using MediatR;

namespace Modules.Scenarios.Application.UseCases.DeleteScenario;

public sealed record DeleteScenarioCommand(Guid ScenarioId) : IRequest<bool>;