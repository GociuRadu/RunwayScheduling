using MediatR;
using Modules.Scenarios.Application;
using Modules.Scenarios.Application.UseCases.RandomEvents;
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
        var cfg = await _configStore.GetById(request.ScenarioConfigId, ct);
        RandomEventCommandValidator.Validate(cfg, request.Name, request.StartTime, request.EndTime, request.ImpactPercent);

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
