using MediatR;
using Modules.Aircrafts.Application.UseCases.GenerateRandomAircraft;
using Modules.Aircrafts.Domain;
using Modules.Scenarios.Application;
using Modules.Scenarios.Application.Services;
using Modules.Scenarios.Application.UseCases.CreateFlights;
using Modules.Scenarios.Domain;
using Modules.Scenarios.Domain.Exceptions;

namespace RunwayScheduling.Tests.Scenarios;

public sealed class CreateFlightsHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IScenarioConfigStore _scenarioConfigStore = Substitute.For<IScenarioConfigStore>();
    private readonly IFlightStore _flightStore = Substitute.For<IFlightStore>();

    [Fact]
    public async Task Handle_GeneratesFlightsAndPersistsThem()
    {
        var scenarioConfigId = Guid.NewGuid();
        var config = CreateScenarioConfig(scenarioConfigId);
        var aircraft = CreateAircraft(config, 4);
        var sut = new CreateFlightsHandler(_mediator, _scenarioConfigStore, _flightStore, new FlightScheduler());

        _scenarioConfigStore.GetById(scenarioConfigId, Arg.Any<CancellationToken>()).Returns(config);
        _mediator.Send(Arg.Any<GenerateRandomAircraftCommand>(), Arg.Any<CancellationToken>()).Returns(aircraft);

        var result = await sut.Handle(new CreateFlightsCommand(scenarioConfigId), CancellationToken.None);

        Assert.Equal(5, result.Count);
        Assert.All(result, flight => Assert.Equal(scenarioConfigId, flight.ScenarioConfigId));
        await _flightStore.Received(1).AddRange(Arg.Is<List<Flight>>(flights => flights.Count == result.Count), Arg.Any<CancellationToken>());
        await _flightStore.Received(1).SaveChanges(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenGeneratedAircraftCountIsTooSmall()
    {
        var scenarioConfigId = Guid.NewGuid();
        var config = CreateScenarioConfig(scenarioConfigId);
        var sut = new CreateFlightsHandler(_mediator, _scenarioConfigStore, _flightStore, new FlightScheduler());

        _scenarioConfigStore.GetById(scenarioConfigId, Arg.Any<CancellationToken>()).Returns(config);
        _mediator.Send(Arg.Any<GenerateRandomAircraftCommand>(), Arg.Any<CancellationToken>()).Returns(CreateAircraft(config, 3));

        var exception = await Assert.ThrowsAsync<InvalidScenarioConfigException>(() => sut.Handle(new CreateFlightsCommand(scenarioConfigId), CancellationToken.None));
        Assert.Equal("Generated aircraft count must be >= ScenarioConfig.AircraftCount", exception.Message);
    }

    [Fact]
    public async Task Handle_Throws_WhenScenarioConfigIsInvalid()
    {
        var scenarioConfigId = Guid.NewGuid();
        var config = CreateScenarioConfig(scenarioConfigId);
        config.InboundAircraftCount = 1;
        var sut = new CreateFlightsHandler(_mediator, _scenarioConfigStore, _flightStore, new FlightScheduler());

        _scenarioConfigStore.GetById(scenarioConfigId, Arg.Any<CancellationToken>()).Returns(config);

        var exception = await Assert.ThrowsAsync<InvalidScenarioConfigException>(() => sut.Handle(new CreateFlightsCommand(scenarioConfigId), CancellationToken.None));
        Assert.Contains("must equal AircraftCount", exception.Message);
    }

    private static ScenarioConfig CreateScenarioConfig(Guid scenarioConfigId) =>
        TestEntityFactory.WithId(
            new ScenarioConfig
            {
                AirportId = Guid.NewGuid(),
                Name = "Scenario",
                StartTime = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                Seed = 123,
                AircraftCount = 4,
                AircraftDifficulty = 2,
                OnGroundAircraftCount = 2,
                InboundAircraftCount = 2,
                RemainingOnGroundAircraftCount = 1
            },
            scenarioConfigId);

    private static List<Aircraft> CreateAircraft(ScenarioConfig config, int count) =>
        Enumerable.Range(1, count)
            .Select(index => new Aircraft
            {
                ScenarioConfigId = config.Id,
                TailNumber = $"YR-{index:000}",
                Model = "A320",
                MaxPassengers = 180,
                WakeCategory = WakeTurbulenceCategory.Medium
            })
            .ToList();
}
