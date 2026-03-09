using MediatR;

namespace Modules.Scenarios.Application.UseCases.DeleteScenario;

public sealed class DeleteScenarioHandler : IRequestHandler<DeleteScenarioCommand, bool>
{
    public readonly IScenarioConfigStore _store;

    public DeleteScenarioHandler(IScenarioConfigStore store) => _store = store;

    public async Task<bool> Handle(DeleteScenarioCommand request, CancellationToken ct)
    {
        return await _store.Delete(request.ScenarioId, ct);
    }
}