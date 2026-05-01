using MediatR;
using Modules.Aircrafts.Application.UseCases.GenerateRandomAircraft;
using Modules.Aircrafts.Domain;
using Modules.Scenarios.Application;
using Modules.Scenarios.Application.Services;
using Modules.Scenarios.Application.UseCases.CreateFlights;
using NSubstitute;
using RunwayScheduling.Tests.Helpers.Builders;

namespace RunwayScheduling.Tests.Unit.Scenarios;

public sealed class CreateFlightsHandlerTests
{
    private readonly IScenarioConfigStore _configStore = Substitute.For<IScenarioConfigStore>();
    private readonly IFlightStore _flightStore = Substitute.For<IFlightStore>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly CreateFlightsHandler _sut;

    public CreateFlightsHandlerTests()
    {
        _sut = new CreateFlightsHandler(_mediator, _configStore, _flightStore, new FlightScheduler());
    }

    [Fact]
    public async Task Handle_ValidConfig_ReturnsFlightsWithCorrectCount()
    {
        var cfg = new ScenarioConfigBuilder().WithAircraftCount(4, 2, 2).Build();
        var command = new CreateFlightsCommand(cfg.Id);
        _configStore.GetById(cfg.Id, Arg.Any<CancellationToken>()).Returns(cfg);
        var aircrafts = Enumerable.Range(0, 4)
            .Select(_ => new Aircraft { ScenarioConfigId = cfg.Id })
            .ToList();
        _mediator.Send(Arg.Any<GenerateRandomAircraftCommand>(), Arg.Any<CancellationToken>())
            .Returns(aircrafts);

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task Handle_ScenarioNotFound_ThrowsException()
    {
        var command = new CreateFlightsCommand(Guid.NewGuid());
        _configStore.GetById(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Modules.Scenarios.Domain.ScenarioConfig?)null);

        await Assert.ThrowsAnyAsync<Exception>(() => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OnGroundPlusInboundNotEqualAircraftCount_ThrowsException()
    {
        // onGround(3) + inbound(2) = 5 != aircraftCount(4)
        var cfg = new ScenarioConfigBuilder().WithAircraftCount(4, 3, 2).Build();
        _configStore.GetById(cfg.Id, Arg.Any<CancellationToken>()).Returns(cfg);
        var command = new CreateFlightsCommand(cfg.Id);

        await Assert.ThrowsAnyAsync<Exception>(() => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RemainingOnGroundOutOfRange_ThrowsException()
    {
        // remainingOnGround(10) > aircraftCount(4)
        var cfg = new ScenarioConfigBuilder()
            .WithAircraftCount(4, 2, 2)
            .WithRemainingOnGround(10)
            .Build();
        _configStore.GetById(cfg.Id, Arg.Any<CancellationToken>()).Returns(cfg);
        var command = new CreateFlightsCommand(cfg.Id);

        await Assert.ThrowsAnyAsync<Exception>(() => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ScenarioDurationUnder10Minutes_ThrowsException()
    {
        var start = DateTime.UtcNow;
        var cfg = new ScenarioConfigBuilder()
            .WithAircraftCount(4, 2, 2)
            .WithTimeWindow(start, start.AddMinutes(5))
            .Build();
        _configStore.GetById(cfg.Id, Arg.Any<CancellationToken>()).Returns(cfg);
        var command = new CreateFlightsCommand(cfg.Id);

        await Assert.ThrowsAnyAsync<Exception>(() => _sut.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GeneratedAircraftCountBelowExpected_ThrowsException()
    {
        var cfg = new ScenarioConfigBuilder().WithAircraftCount(4, 2, 2).Build();
        _configStore.GetById(cfg.Id, Arg.Any<CancellationToken>()).Returns(cfg);
        _mediator.Send(Arg.Any<GenerateRandomAircraftCommand>(), Arg.Any<CancellationToken>())
            .Returns(new List<Aircraft>()); // empty — less than 4
        var command = new CreateFlightsCommand(cfg.Id);

        await Assert.ThrowsAnyAsync<Exception>(() => _sut.Handle(command, CancellationToken.None));
    }
}
