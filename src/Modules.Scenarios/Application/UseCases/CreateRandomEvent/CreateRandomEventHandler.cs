using MediatR;
using Modules.Scenarios.Application;
using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application.UseCases.CreateRandomEvent;

public sealed class CreateRandomEventHandler
    : IRequestHandler<CreateRandomEventCommand, RandomEvent>
{
    private readonly IRandomEventStore _randomEventStore;
    private readonly IScenarioConfigStore _configStore;

    public CreateRandomEventHandler(
        IRandomEventStore randomEventStore,
        IScenarioConfigStore configStore)
    {
        _randomEventStore = randomEventStore;
        _configStore = configStore;
    }

    public async Task<RandomEvent> Handle(CreateRandomEventCommand request, CancellationToken ct)
    {
        // TODO: SECURITY: Validation only checks null/ordering/basic range. Enforce scenario-window bounds, max lengths, and a central validator so invalid payloads cannot reach persistence as 500-level exceptions.
        var cfg = await _configStore.GetById(request.ScenarioConfigId, ct);
        if (cfg is null)
            throw new Exception("Scenario config not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new Exception("Name is required.");

        if (request.StartTime >= request.EndTime)
            throw new Exception("StartTime must be earlier than EndTime.");

        if (request.ImpactPercent < 0)
            throw new Exception("ImpactPercent must be greater than or equal to 0.");

        var randomEvent = new RandomEvent
        {
            ScenarioConfigId = request.ScenarioConfigId,
            Name = request.Name,
            Description = request.Description,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            ImpactPercent = request.ImpactPercent
        };

        return await _randomEventStore.Add(randomEvent, ct);
    }
}
