using MediatR;
using Modules.Scenarios.Application;
using Modules.Scenarios.Application.UseCases.RandomEvents;
using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application.UseCases.UpdateRandomEvent;

public sealed class UpdateRandomEventHandler
    : IRequestHandler<UpdateRandomEventCommand, RandomEvent?>
{
    private readonly IRandomEventStore _randomEventStore;
    private readonly IScenarioConfigStore _configStore;

    public UpdateRandomEventHandler(
        IRandomEventStore randomEventStore,
        IScenarioConfigStore configStore)
    {
        _randomEventStore = randomEventStore;
        _configStore = configStore;
    }

    public async Task<RandomEvent?> Handle(UpdateRandomEventCommand request, CancellationToken ct)
    {
        var cfg = await _configStore.GetById(request.ScenarioConfigId, ct);
        RandomEventCommandValidator.Validate(cfg, request.Name, request.StartTime, request.EndTime, request.ImpactPercent);

        return await _randomEventStore.Update(
            request.Id,
            request.ScenarioConfigId,
            request.Name,
            request.Description,
            request.StartTime,
            request.EndTime,
            request.ImpactPercent,
            ct
        );
    }
}
