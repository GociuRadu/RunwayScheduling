using Modules.Scenarios.Domain;

namespace RunwayScheduling.Tests.Helpers.Builders;

public sealed class ScenarioConfigBuilder
{
    private int _aircraftCount = 4;
    private int _onGround = 2;
    private int _inbound = 2;
    private int _remainingOnGround = 1;
    private int _difficulty = 1;
    private DateTime _start = DateTime.UtcNow.Date.AddHours(8);
    private DateTime _end = DateTime.UtcNow.Date.AddHours(10);
    private int _seed = 42;

    public ScenarioConfigBuilder WithAircraftCount(int total, int onGround, int inbound)
    {
        _aircraftCount = total; _onGround = onGround; _inbound = inbound;
        return this;
    }
    public ScenarioConfigBuilder WithRemainingOnGround(int n) { _remainingOnGround = n; return this; }
    public ScenarioConfigBuilder WithDifficulty(int d) { _difficulty = d; return this; }
    public ScenarioConfigBuilder WithTimeWindow(DateTime start, DateTime end) { _start = start; _end = end; return this; }
    public ScenarioConfigBuilder WithSeed(int seed) { _seed = seed; return this; }

    public ScenarioConfig Build() => new()
    {
        AirportId = Guid.NewGuid(),
        Name = "Test Scenario",
        Difficulty = _difficulty,
        AircraftCount = _aircraftCount,
        AircraftDifficulty = 1,
        OnGroundAircraftCount = _onGround,
        InboundAircraftCount = _inbound,
        RemainingOnGroundAircraftCount = _remainingOnGround,
        StartTime = _start,
        EndTime = _end,
        Seed = _seed
    };
}
