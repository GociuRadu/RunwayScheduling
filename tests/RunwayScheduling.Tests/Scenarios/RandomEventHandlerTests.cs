using Modules.Scenarios.Application;
using Modules.Scenarios.Application.UseCases.CreateRandomEvent;
using Modules.Scenarios.Application.UseCases.UpdateRandomEvent;
using Modules.Scenarios.Domain;

namespace RunwayScheduling.Tests.Scenarios;

public sealed class RandomEventHandlerTests
{
    private readonly IRandomEventStore _randomEventStore = Substitute.For<IRandomEventStore>();
    private readonly IScenarioConfigStore _scenarioConfigStore = Substitute.For<IScenarioConfigStore>();

    [Fact]
    public async Task CreateHandle_Throws_WhenNameIsMissing()
    {
        var scenarioConfigId = Guid.NewGuid();
        var sut = new CreateRandomEventHandler(_randomEventStore, _scenarioConfigStore);

        _scenarioConfigStore.GetById(scenarioConfigId, Arg.Any<CancellationToken>()).Returns(new ScenarioConfig());

        var exception = await Assert.ThrowsAsync<Exception>(() => sut.Handle(
            new CreateRandomEventCommand(scenarioConfigId, "", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5), 10),
            CancellationToken.None));
        Assert.Equal("Name is required.", exception.Message);
    }

    [Fact]
    public async Task UpdateHandle_CallsStoreWithValidatedPayload()
    {
        var scenarioConfigId = Guid.NewGuid();
        var randomEventId = Guid.NewGuid();
        var request = new UpdateRandomEventCommand(
            randomEventId,
            scenarioConfigId,
            "Inspection",
            "Runway inspection",
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(5),
            20);
        var expected = new RandomEvent { Name = request.Name, ImpactPercent = request.ImpactPercent };
        var sut = new UpdateRandomEventHandler(_randomEventStore, _scenarioConfigStore);

        _scenarioConfigStore.GetById(scenarioConfigId, Arg.Any<CancellationToken>()).Returns(new ScenarioConfig());
        _randomEventStore.Update(
            request.Id,
            request.ScenarioConfigId,
            request.Name,
            request.Description,
            request.StartTime,
            request.EndTime,
            request.ImpactPercent,
            Arg.Any<CancellationToken>()).Returns(expected);

        var result = await sut.Handle(request, CancellationToken.None);

        Assert.Same(expected, result);
    }
}
