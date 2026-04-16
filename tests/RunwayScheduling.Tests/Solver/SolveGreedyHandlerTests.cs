using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Application.Scheduling;
using Modules.Solver.Application.Snapshot;
using Modules.Solver.Application.UseCases.SolveGreedy;
using Modules.Solver.Domain;

namespace RunwayScheduling.Tests.Solver;

public sealed class SolveGreedyHandlerTests
{
    private readonly IScenarioSnapshotFactory _snapshotFactory = Substitute.For<IScenarioSnapshotFactory>();
    private readonly ISchedulingEngine _engine = Substitute.For<ISchedulingEngine>();

    [Fact]
    public async Task Handle_SortsFlightsBeforeCallingEngine()
    {
        var scenarioConfigId = Guid.NewGuid();
        var snapshot = CreateSnapshot(scenarioConfigId);
        var evaluation = new SchedulingEvaluation { Fitness = 42 };
        var expected = new SolverResult
        {
            ScenarioConfigId = scenarioConfigId,
            AlgorithmName = "Greedy",
            Fitness = 42
        };

        _snapshotFactory.CreateAsync(scenarioConfigId, Arg.Any<CancellationToken>()).Returns(snapshot);
        _engine
            .Evaluate(
                Arg.Is<IReadOnlyList<(Flight Flight, Guid SourceId)>>(flights =>
                    flights.Count == 3
                    && flights[0].Flight.Callsign == "EARLY"
                    && flights[1].Flight.Callsign == "PRIORITY"
                    && flights[2].Flight.Callsign == "LATE"),
                Arg.Any<PreparedScenario>())
            .Returns(evaluation);
        _engine.CreateResult(evaluation, scenarioConfigId, "Greedy", Arg.Any<double>()).Returns(expected);

        var sut = new SolveGreedyHandler(_snapshotFactory, _engine);

        var result = await sut.Handle(new SolveGreedyQuery(scenarioConfigId), CancellationToken.None);

        Assert.Same(expected, result);
        await _snapshotFactory.Received(1).CreateAsync(scenarioConfigId, Arg.Any<CancellationToken>());
        _engine.Received(1).CreateResult(evaluation, scenarioConfigId, "Greedy", Arg.Any<double>());
    }

    private static ScenarioSnapshot CreateSnapshot(Guid scenarioConfigId)
    {
        var airportId = Guid.NewGuid();

        return new ScenarioSnapshot
        {
            ScenarioConfig = TestEntityFactory.WithId(new ScenarioConfig
            {
                AirportId = airportId,
                Name = "Scenario",
                StartTime = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            }, scenarioConfigId),
            Airport = new Airport { Name = "LROP" },
            Runways =
            [
                new Runway { AirportId = airportId, Name = "08L", IsActive = true, RunwayType = RunwayType.Both }
            ],
            RunwaySourceIds = [Guid.NewGuid()],
            Flights =
            [
                new Flight
                {
                    Callsign = "LATE",
                    ScheduledTime = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                    Priority = 1
                },
                new Flight
                {
                    Callsign = "EARLY",
                    ScheduledTime = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                    Priority = 1
                },
                new Flight
                {
                    Callsign = "PRIORITY",
                    ScheduledTime = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                    Priority = 5
                }
            ],
            FlightSourceIds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],
            RandomEvents = [],
            WeatherIntervals = []
        };
    }
}
