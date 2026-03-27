using MediatR;
using Modules.Airports.Application;
using Modules.Airports.Application.UseCases.GetRunwaysByAirportId;
using Modules.Airports.Domain;
using Modules.Scenarios.Application;
using Modules.Scenarios.Application.UseCases.GetFlights;
using Modules.Scenarios.Application.UseCases.GetRandomEventsByScenarioConfigId;
using Modules.Scenarios.Application.UseCases.GetWeatherIntervals;
using Modules.Scenarios.Domain;
using Modules.Solver.Application;

namespace RunwayScheduling.Tests.Solver;

public sealed class ScenarioSnapshotLoaderTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IScenarioConfigStore _scenarioConfigStore = Substitute.For<IScenarioConfigStore>();
    private readonly IAirportStore _airportStore = Substitute.For<IAirportStore>();

    [Fact]
    public async Task Load_MapsQueriesIntoScenarioSnapshot()
    {
        var scenarioConfigId = Guid.NewGuid();
        var airportId = Guid.NewGuid();
        var scenarioConfig = new ScenarioConfig
        {
            AirportId = airportId,
            StartTime = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2025, 1, 1, 16, 0, 0, DateTimeKind.Utc)
        };
        var airport = new Airport { Name = "OTP" };
        var sut = new ScenarioSnapshotLoader(_sender, _scenarioConfigStore, _airportStore);

        _scenarioConfigStore.GetById(scenarioConfigId, Arg.Any<CancellationToken>()).Returns(scenarioConfig);
        _airportStore.GetAll().Returns([TestEntityFactory.WithId(airport, airportId)]);
        _sender.Send(Arg.Any<GetRandomEventsByScenarioConfigIdQuery>(), Arg.Any<CancellationToken>()).Returns(
            [
                new RandomEventDto(Guid.NewGuid(), scenarioConfigId, "Inspection", "Desc", scenarioConfig.StartTime, scenarioConfig.StartTime.AddMinutes(30), 15)
            ]);
        _sender.Send(Arg.Any<FlightQuery>(), Arg.Any<CancellationToken>()).Returns(
            [
                new FlightDto(Guid.NewGuid(), scenarioConfigId, Guid.NewGuid(), "FLT001", FlightType.Arrival, scenarioConfig.StartTime.AddMinutes(10), 10, 0, 3)
            ]);
        _sender.Send(Arg.Any<WeatherIntervalsQuery>(), Arg.Any<CancellationToken>()).Returns(
            [
                new WeatherIntervalsDto(Guid.NewGuid(), scenarioConfigId, scenarioConfig.StartTime, scenarioConfig.EndTime, WeatherCondition.Clear)
            ]);
        _sender.Send(Arg.Any<GetRunwaysByAirportIdQuery>(), Arg.Any<CancellationToken>()).Returns(
            [
                new RunwayDto(Guid.NewGuid(), airportId, "RWY-1", true, RunwayType.Both)
            ]);

        var result = await sut.Load(scenarioConfigId, CancellationToken.None);

        Assert.Same(scenarioConfig, result.ScenarioConfig);
        Assert.Equal(airportId, result.Airport.Id);
        Assert.Single(result.Flights, flight => flight.Callsign == "FLT001");
        Assert.Single(result.RandomEvents, randomEvent => randomEvent.Name == "Inspection");
        Assert.Single(result.WeatherIntervals, interval => interval.WeatherType == WeatherCondition.Clear);
        Assert.Single(result.Runways, runway => runway.Name == "RWY-1");
    }

    [Fact]
    public async Task Load_Throws_WhenScenarioConfigDoesNotExist()
    {
        var scenarioConfigId = Guid.NewGuid();
        var sut = new ScenarioSnapshotLoader(_sender, _scenarioConfigStore, _airportStore);

        _scenarioConfigStore.GetById(scenarioConfigId, Arg.Any<CancellationToken>()).Returns((ScenarioConfig?)null);

        var exception = await Assert.ThrowsAsync<Exception>(() => sut.Load(scenarioConfigId, CancellationToken.None));
        Assert.Equal("Scenario config not found.", exception.Message);
    }

    [Fact]
    public async Task Load_Throws_WhenAirportDoesNotExist()
    {
        var scenarioConfigId = Guid.NewGuid();
        var sut = new ScenarioSnapshotLoader(_sender, _scenarioConfigStore, _airportStore);

        _scenarioConfigStore.GetById(scenarioConfigId, Arg.Any<CancellationToken>()).Returns(new ScenarioConfig
        {
            AirportId = Guid.NewGuid()
        });
        _airportStore.GetAll().Returns([]);

        var exception = await Assert.ThrowsAsync<Exception>(() => sut.Load(scenarioConfigId, CancellationToken.None));
        Assert.Equal("Airport not found.", exception.Message);
    }
}
