using MediatR;
using Modules.Scenarios.Application;

namespace Modules.Scenarios.Application.UseCases.DeleteRandomEvent;

public sealed class DeleteRandomEventHandler : IRequestHandler<DeleteRandomEventCommand, bool>
{
    private readonly IRandomEventStore _randomEvent;

    public DeleteRandomEventHandler(IRandomEventStore randomEvent)
    {
        _randomEvent = randomEvent;
    }

    public async Task<bool> Handle(DeleteRandomEventCommand req, CancellationToken ct)
    {
        return await _randomEvent.Delete(req.Id, ct);
    }
}