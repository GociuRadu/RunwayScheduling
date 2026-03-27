using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Application.GreedySolver;
using Modules.Solver.Domain;

namespace RunwayScheduling.Tests.Solver;

public sealed class GreedyScenarioSolverTests
{
    private readonly GreedyScenarioSolver _sut = new();

    private static ScenarioSnapshot BuildSnapshot(
        List<Flight> flights,
        int baseSeparationSeconds = 60,
        int wakePercent = 100,
        int weatherPercent = 100,
        DateTime? start = null,
        DateTime? end = null)
    {
        var baseTime = start ?? new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        return new ScenarioSnapshot
        {
            ScenarioConfig = new ScenarioConfig
            {
                BaseSeparationSeconds = baseSeparationSeconds,
                WakePercent = wakePercent,
                WeatherPercent = weatherPercent,
                StartTime = baseTime,
                EndTime = end ?? baseTime.AddHours(8)
            },
            Airport = new Airport { Name = "OTP" },
            Flights = flights
        };
    }

    private static Flight MakeFlight(DateTime scheduledTime, int maxDelay = 60, int priority = 1) =>
        new()
        {
            Callsign = $"FLT{scheduledTime:HHmm}",
            ScheduledTime = scheduledTime,
            MaxDelayMinutes = maxDelay,
            Priority = priority,
            Type = FlightType.Arrival
        };

    [Fact]
    public void Solve_WithNoFlights_ReturnsEmptyResult()
    {
        var snapshot = BuildSnapshot([]);

        var result = _sut.Solve(snapshot);

        Assert.Empty(result.Flights);
        Assert.Equal(0, result.TotalFlights);
        Assert.Equal(0, result.TotalDelayMinutes);
    }

    [Fact]
    public void Solve_SingleFlight_IsScheduledWithNoDelay()
    {
        var time = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var snapshot = BuildSnapshot([MakeFlight(time)], start: time.AddHours(-1));

        var result = _sut.Solve(snapshot);

        Assert.Single(result.Flights);
        Assert.Equal(0, result.Flights[0].DelayMinutes);
        Assert.Equal(time, result.Flights[0].AssignedTime);
    }

    [Fact]
    public void Solve_SecondFlight_IsDelayedBySeparation()
    {
        var baseTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var flights = new List<Flight>
        {
            MakeFlight(baseTime, maxDelay: 120),
            MakeFlight(baseTime, maxDelay: 120)  // same time — second must wait
        };
        var snapshot = BuildSnapshot(flights, baseSeparationSeconds: 60, start: baseTime.AddHours(-1));

        var result = _sut.Solve(snapshot);

        var scheduled = result.Flights.Where(f => f.AssignedTime != null).ToList();
        Assert.Equal(2, scheduled.Count);
        // second flight assigned 60s after first
        var times = scheduled.Select(f => f.AssignedTime!.Value).OrderBy(t => t).ToList();
        Assert.Equal(TimeSpan.FromSeconds(60), times[1] - times[0]);
    }

    [Fact]
    public void Solve_FlightExceedingMaxDelay_IsCanceled()
    {
        var baseTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var flights = new List<Flight>
        {
            MakeFlight(baseTime, maxDelay: 120),
            MakeFlight(baseTime, maxDelay: 0)  // zero tolerance — will exceed separation delay
        };
        var snapshot = BuildSnapshot(flights, baseSeparationSeconds: 60, start: baseTime.AddHours(-1));

        var result = _sut.Solve(snapshot);

        Assert.Contains(result.Flights, f => f.AssignedTime == null);
        Assert.Equal(1, result.TotalCanceledFlights);
    }

    [Fact]
    public void Solve_FlightOutsideScenarioWindow_IsCanceled()
    {
        var baseTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        // Flight scheduled before window
        var flight = MakeFlight(baseTime.AddHours(-2), maxDelay: 0);
        var snapshot = BuildSnapshot([flight], start: baseTime, end: baseTime.AddHours(4));

        var result = _sut.Solve(snapshot);

        Assert.Equal(1, result.TotalCanceledFlights);
        Assert.Null(result.Flights[0].AssignedTime);
    }

    [Fact]
    public void Solve_FlightsOrderedByScheduledTimeThenPriority()
    {
        var baseTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var low = MakeFlight(baseTime, maxDelay: 120, priority: 1);
        var high = MakeFlight(baseTime, maxDelay: 120, priority: 5);
        var snapshot = BuildSnapshot([low, high], start: baseTime.AddHours(-1));

        var result = _sut.Solve(snapshot);

        // high priority should be scheduled first (assigned time = baseTime, no delay)
        var highResult = result.Flights.First(f => f.Priority == 5);
        Assert.Equal(0, highResult.DelayMinutes);
    }

    [Fact]
    public void Solve_SeparationScaledByWakeAndWeatherPercent()
    {
        var baseTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var flights = new List<Flight>
        {
            MakeFlight(baseTime, maxDelay: 120),
            MakeFlight(baseTime, maxDelay: 120)
        };
        // base=100s, wake=50%, weather=100% => effective = 50s
        var snapshot = BuildSnapshot(flights, baseSeparationSeconds: 100, wakePercent: 50, weatherPercent: 100,
            start: baseTime.AddHours(-1));

        var result = _sut.Solve(snapshot);

        var times = result.Flights
            .Where(f => f.AssignedTime != null)
            .Select(f => f.AssignedTime!.Value)
            .OrderBy(t => t)
            .ToList();

        Assert.Equal(2, times.Count);
        Assert.Equal(TimeSpan.FromSeconds(50), times[1] - times[0]);
    }

    [Fact]
    public void Solve_ResultCountersMatchFlights()
    {
        var baseTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var flights = new List<Flight>
        {
            MakeFlight(baseTime, maxDelay: 120),
            MakeFlight(baseTime, maxDelay: 120),
            MakeFlight(baseTime, maxDelay: 0)  // will be canceled
        };
        var snapshot = BuildSnapshot(flights, baseSeparationSeconds: 60, start: baseTime.AddHours(-1));

        var result = _sut.Solve(snapshot);

        Assert.Equal(3, result.TotalFlights);
        Assert.Equal(1, result.TotalCanceledFlights);
        Assert.True(result.TotalDelayMinutes >= 0);
    }
}
