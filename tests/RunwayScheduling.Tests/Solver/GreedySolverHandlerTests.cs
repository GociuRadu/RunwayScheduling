using Modules.Solver.Application;
using Modules.Solver.Application.GreedySolver;
using Modules.Solver.Domain;

namespace RunwayScheduling.Tests.Solver;

public sealed class GreedySolverHandlerTests
{
    [Fact]
    public async Task Handle_LoadsSnapshotAndReturnsSolverResult()
    {
        var scenarioConfigId = Guid.NewGuid();
        var snapshotLoader = Substitute.For<IScenarioSnapshotLoader>();
        var solver = new GreedyScenarioSolver();
        var snapshot = new ScenarioSnapshot
        {
            ScenarioConfig = new Modules.Scenarios.Domain.ScenarioConfig
            {
                StartTime = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            },
            Airport = new Modules.Airports.Domain.Airport { Name = "OTP" },
            Runways =
            [
                new Modules.Airports.Domain.Runway
                {
                    AirportId = Guid.NewGuid(),
                    Name = "RWY-1",
                    IsActive = true,
                    RunwayType = Modules.Airports.Domain.RunwayType.Both
                }
            ],
            Flights =
            [
                new Modules.Scenarios.Domain.Flight
                {
                    ScenarioConfigId = scenarioConfigId,
                    AircraftId = Guid.NewGuid(),
                    Callsign = "FLT001",
                    Type = Modules.Scenarios.Domain.FlightType.Arrival,
                    ScheduledTime = new DateTime(2025, 1, 1, 8, 15, 0, DateTimeKind.Utc),
                    MaxDelayMinutes = 5,
                    Priority = 1
                }
            ]
        };
        var sut = new GreedySolverHandler(snapshotLoader, solver);

        snapshotLoader.Load(scenarioConfigId, Arg.Any<CancellationToken>()).Returns(snapshot);

        var result = await sut.Handle(new GreedySolverQuery(scenarioConfigId), CancellationToken.None);

        Assert.Equal("Greedy", result.AlgorithmName);
        Assert.Equal(1, result.TotalFlights);
        await snapshotLoader.Received(1).Load(scenarioConfigId, Arg.Any<CancellationToken>());
    }
}
