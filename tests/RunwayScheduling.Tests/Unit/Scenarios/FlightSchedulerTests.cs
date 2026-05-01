using Modules.Aircrafts.Domain;
using Modules.Scenarios.Application.Services;
using Modules.Scenarios.Domain;
using RunwayScheduling.Tests.Helpers.Builders;

namespace RunwayScheduling.Tests.Unit.Scenarios;

public sealed class FlightSchedulerTests
{
    private readonly FlightScheduler _sut = new();

    private static List<Aircraft> MakeAircrafts(ScenarioConfig cfg)
        => Enumerable.Range(0, cfg.AircraftCount)
            .Select((_, i) => new Aircraft
            {
                ScenarioConfigId = cfg.Id,
                WakeCategory = WakeTurbulenceCategory.Light
            })
            .ToList();

    [Fact]
    public void Schedule_ValidInputs_ReturnsCorrectTotalFlightCount()
    {
        // 2 onGround + 2 inbound, remainingOnGround=1 → 1 stay, 3 depart + 2 arrivals
        var cfg = new ScenarioConfigBuilder().WithAircraftCount(4, 2, 2).WithRemainingOnGround(1).Build();
        var aircrafts = MakeAircrafts(cfg);

        var flights = _sut.Schedule(aircrafts, cfg);

        Assert.NotEmpty(flights);
        Assert.Equal(2, flights.Count(f => f.Type == FlightType.Arrival));
    }

    [Fact]
    public void Schedule_OnlyInboundAircrafts_ReturnsOnlyArrivalFlights()
    {
        // all inbound, none depart (remainingOnGround = aircraftCount)
        var cfg = new ScenarioConfigBuilder()
            .WithAircraftCount(4, 0, 4)
            .WithRemainingOnGround(4)
            .Build();
        var aircrafts = MakeAircrafts(cfg);

        var flights = _sut.Schedule(aircrafts, cfg);

        Assert.All(flights, f => Assert.Equal(FlightType.Arrival, f.Type));
    }

    [Fact]
    public void Schedule_AllFlightsWithinSafeWindow()
    {
        var cfg = new ScenarioConfigBuilder().WithAircraftCount(4, 2, 2).Build();
        var aircrafts = MakeAircrafts(cfg);
        var (safeStart, safeEnd) = FlightScheduler.GetSafeWindow(cfg.StartTime, cfg.EndTime, 10);

        var flights = _sut.Schedule(aircrafts, cfg);

        Assert.All(flights, f =>
        {
            Assert.True(f.ScheduledTime >= safeStart, $"Flight {f.Callsign} starts before safe window");
            Assert.True(f.ScheduledTime <= safeEnd, $"Flight {f.Callsign} ends after safe window");
        });
    }

    [Fact]
    public void Schedule_FlightsSortedByScheduledTime()
    {
        var cfg = new ScenarioConfigBuilder().WithAircraftCount(4, 2, 2).Build();
        var flights = _sut.Schedule(MakeAircrafts(cfg), cfg);

        for (int i = 1; i < flights.Count; i++)
            Assert.True(flights[i].ScheduledTime >= flights[i - 1].ScheduledTime);
    }

    [Fact]
    public void Schedule_DepartureTurnaroundRespected()
    {
        // aircraft that has both arrival and departure must have 20min gap
        var cfg = new ScenarioConfigBuilder()
            .WithAircraftCount(2, 1, 1)
            .WithRemainingOnGround(0)
            .WithTimeWindow(DateTime.UtcNow.Date.AddHours(8), DateTime.UtcNow.Date.AddHours(12))
            .Build();
        var aircrafts = MakeAircrafts(cfg);

        var flights = _sut.Schedule(aircrafts, cfg);

        var arrivals = flights.Where(f => f.Type == FlightType.Arrival).ToDictionary(f => f.AircraftId);
        var departures = flights.Where(f => f.Type == FlightType.Departure);
        foreach (var dep in departures)
        {
            if (arrivals.TryGetValue(dep.AircraftId, out var arr))
                Assert.True(dep.ScheduledTime >= arr.ScheduledTime.AddMinutes(20),
                    $"Departure {dep.Callsign} is less than 20 min after arrival");
        }
    }

    [Fact]
    public void Schedule_EmptyAircraftList_ReturnsEmptyList()
    {
        var cfg = new ScenarioConfigBuilder().WithAircraftCount(0, 0, 0).WithRemainingOnGround(0).Build();

        var flights = _sut.Schedule(new List<Aircraft>(), cfg);

        Assert.Empty(flights);
    }

    [Fact]
    public void Schedule_DeterministicWithSameSeed_ReturnsSameFlights()
    {
        // same config (same Seed) must produce identical results
        var cfg = new ScenarioConfigBuilder().WithAircraftCount(4, 2, 2).WithSeed(12345).Build();
        var aircrafts1 = MakeAircrafts(cfg);
        var aircrafts2 = MakeAircrafts(cfg);

        var flights1 = _sut.Schedule(aircrafts1, cfg);
        var flights2 = _sut.Schedule(aircrafts2, cfg);

        Assert.Equal(flights1.Count, flights2.Count);
        for (int i = 0; i < flights1.Count; i++)
            Assert.Equal(flights1[i].ScheduledTime, flights2[i].ScheduledTime);
    }
}
