using MediatR;
using Modules.Scenarios.Application;

namespace Modules.Scenarios.Application.UseCases.GetRandomEventsByScenarioConfigId;

public sealed class GetRandomEventsByScenarioConfigIdHandler
    : IRequestHandler<GetRandomEventsByScenarioConfigIdQuery, IReadOnlyList<RandomEventDto>>
{
    private readonly IRandomEventStore _eventStore;

    public GetRandomEventsByScenarioConfigIdHandler(IRandomEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<IReadOnlyList<RandomEventDto>> Handle(
        GetRandomEventsByScenarioConfigIdQuery req,
        CancellationToken ct)
    {
        var randomEvents = await _eventStore.GetAllByScenarioConfigId(req.ScenarioConfigId, ct);
        return randomEvents;
    }
}