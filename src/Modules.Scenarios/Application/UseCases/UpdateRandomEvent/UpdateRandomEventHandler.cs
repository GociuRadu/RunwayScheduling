using MediatR;
using Modules.Scenarios.Application;
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
        if (cfg is null)
            throw new Exception("Scenario config not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("Name is required.");

        if (request.StartTime >= request.EndTime)
            throw new Exception("StartTime must be earlier than EndTime.");

        if (request.ImpactPercent < 0)
            throw new Exception("ImpactPercent must be greater than or equal to 0.");

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