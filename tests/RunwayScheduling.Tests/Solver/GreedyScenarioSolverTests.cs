using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Application.GreedySolver;
using Modules.Solver.Domain;

namespace RunwayScheduling.Tests.Solver;

public sealed class GreedyScenarioSolverTests
{
    private readonly GreedyScenarioSolver _sut = new();

    [Fact]
    public void Solve_WhenNoCompatibleRunway_CancelsFlight()
    {
        var scheduledTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var snapshot = BuildSnapshot(
            [CreateFlight(scheduledTime, FlightType.Arrival)],
            [CreateRunway("RWY-1", RunwayType.Takeoff)]);

        var result = _sut.Solve(snapshot);

        Assert.Equal(1, result.TotalCanceledFlights);
        Assert.Single(result.Flights);
        Assert.Equal(CancellationReason.NoCompatibleRunway, result.Flights[0].CancellationReason);
    }

    [Fact]
    public void Solve_WhenFlightsShareRunway_DelaysSecondFlightBySeparation()
    {
        var scheduledTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var snapshot = BuildSnapshot(
            [
                CreateFlight(scheduledTime, maxDelayMinutes: 120),
                CreateFlight(scheduledTime, maxDelayMinutes: 120)
            ],
            [CreateRunway("RWY-1", RunwayType.Both)],
            baseSeparationSeconds: 60);

        var result = _sut.Solve(snapshot);

        Assert.Equal(0, result.TotalCanceledFlights);
        Assert.Equal(1, result.TotalDelayedFlights);
        Assert.All(result.Flights, flight => Assert.NotNull(flight.AssignedTime));
        Assert.Equal(1, result.Flights.Max(flight => flight.DelayMinutes));
        Assert.Equal("RWY-1", result.Flights.Single(flight => flight.DelayMinutes == 1).AssignedRunway);
    }

    [Fact]
    public void Solve_WhenWeatherAndRandomEventAreActive_UsesScaledSeparation()
    {
        var scheduledTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var scenarioStart = scheduledTime.AddHours(-1);
        var scenarioEnd = scheduledTime.AddHours(1);
        var snapshot = BuildSnapshot(
            [
                CreateFlight(scheduledTime, maxDelayMinutes: 120),
                CreateFlight(scheduledTime, maxDelayMinutes: 120)
            ],
            [CreateRunway("RWY-1", RunwayType.Both)],
            baseSeparationSeconds: 100,
            weatherIntervals:
            [
                new WeatherInterval
                {
                    ScenarioConfigId = Guid.NewGuid(),
                    StartTime = scenarioStart,
                    EndTime = scenarioEnd,
                    WeatherType = WeatherCondition.Cloud
                }
            ],
            randomEvents:
            [
                new RandomEvent
                {
                    ScenarioConfigId = Guid.NewGuid(),
                    Name = "Inspection",
                    Description = "Runway inspection",
                    StartTime = scenarioStart,
                    EndTime = scenarioEnd,
                    ImpactPercent = 50
                }
            ],
            start: scenarioStart,
            end: scenarioEnd);

        var result = _sut.Solve(snapshot);

        Assert.Equal(165, result.Flights.Single(flight => flight.DelayMinutes > 0).SeparationAppliedSeconds);
    }

    [Fact]
    public void Solve_WhenFlightsShareSchedule_OrdersByPriority()
    {
        var scheduledTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var highPriorityFlight = CreateFlight(scheduledTime, maxDelayMinutes: 120, priority: 5);
        var lowPriorityFlight = CreateFlight(scheduledTime, maxDelayMinutes: 120, priority: 1);
        var snapshot = BuildSnapshot(
            [lowPriorityFlight, highPriorityFlight],
            [CreateRunway("RWY-1", RunwayType.Both)],
            baseSeparationSeconds: 60);

        var result = _sut.Solve(snapshot);

        Assert.Equal(0, result.Flights.Single(flight => flight.Priority == 5).DelayMinutes);
        Assert.Equal(1, result.Flights.Single(flight => flight.Priority == 1).DelayMinutes);
    }

    [Fact]
    public void Solve_WhenAssignmentExceedsScenarioWindow_CancelsFlight()
    {
        var scheduledTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var snapshot = BuildSnapshot(
            [CreateFlight(scheduledTime, maxDelayMinutes: 120)],
            [CreateRunway("RWY-1", RunwayType.Both)],
            start: scheduledTime.AddHours(-2),
            end: scheduledTime.AddHours(-1));

        var result = _sut.Solve(snapshot);

        Assert.Equal(1, result.TotalCanceledFlights);
        Assert.Equal(CancellationReason.OutsideScenarioWindow, result.Flights[0].CancellationReason);
    }

    private static ScenarioSnapshot BuildSnapshot(
        IReadOnlyList<Flight> flights,
        IReadOnlyList<Runway> runways,
        int baseSeparationSeconds = 60,
        int wakePercent = 100,
        int weatherPercent = 100,
        IReadOnlyList<WeatherInterval>? weatherIntervals = null,
        IReadOnlyList<RandomEvent>? randomEvents = null,
        DateTime? start = null,
        DateTime? end = null)
    {
        var scenarioStart = start ?? new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var scenarioEnd = end ?? scenarioStart.AddHours(8);

        return new ScenarioSnapshot
        {
            ScenarioConfig = new ScenarioConfig
            {
                BaseSeparationSeconds = baseSeparationSeconds,
                WakePercent = wakePercent,
                WeatherPercent = weatherPercent,
                StartTime = scenarioStart,
                EndTime = scenarioEnd
            },
            Airport = new Airport { Name = "OTP" },
            Runways = runways,
            Flights = flights,
            WeatherIntervals = weatherIntervals ?? [],
            RandomEvents = randomEvents ?? []
        };
    }

    private static Flight CreateFlight(
        DateTime scheduledTime,
        FlightType type = FlightType.Arrival,
        int maxDelayMinutes = 60,
        int priority = 1) =>
        new()
        {
            ScenarioConfigId = Guid.NewGuid(),
            AircraftId = Guid.NewGuid(),
            Callsign = $"FLT{priority}{scheduledTime:HHmm}",
            Type = type,
            ScheduledTime = scheduledTime,
            MaxDelayMinutes = maxDelayMinutes,
            MaxEarlyMinutes = 0,
            Priority = priority
        };

    private static Runway CreateRunway(string name, RunwayType runwayType) =>
        new()
        {
            AirportId = Guid.NewGuid(),
            Name = name,
            IsActive = true,
            RunwayType = runwayType
        };
}
