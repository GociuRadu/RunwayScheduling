using System.Security.Cryptography;
using Modules.Aircrafts.Domain;
namespace Modules.Scenarios.Domain;

public sealed class ScenarioConfig
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid AirportId { get; set; } = Guid.Empty;

    public string Name { get; set; } = string.Empty;
    public int Difficulty { get; set; } = 1;

    public DateTime StartTime { get; set; } = DateTime.Today;
    public DateTime EndTime { get; set; } = DateTime.Today.AddHours(23).AddMinutes(59);

    // Deterministic seed (used by generators)
    public int Seed { get; set; } = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);

    // Aircraft generation (total)
    public int AircraftCount { get; set; } = 20;
    public int AircraftDifficulty { get; set; } = 1;

    // Split of AircraftCount
    // Rules:
    // - AircraftCount = OnGroundAircraftCount + InboundAircraftCount
    // - Departures = OnGroundAircraftCount - RemainingOnGroundAircraftCount
    // - RemainingOnGroundAircraftCount = how many aircraft remain on the airport at the end
    public int OnGroundAircraftCount { get; set; } = 10;
    public int InboundAircraftCount { get; set; } = 10;
    public int RemainingOnGroundAircraftCount { get; set; } = 5;

    // Base time used by scheduler (e.g., seconds of separation/occupancy)
    public int BaseSeparationSeconds { get; set; } = 45;

    // Multipliers (percents) applied on top of BaseSeparationSeconds
    public int WakePercent { get; set; } = 100;    // multiply by (WakePercent / 100)
    public int WeatherPercent { get; set; } = 100; // multiply by (WeatherPercent / 100)

    // Weather generation settings
    public int WeatherIntervalCount { get; set; } = 4;
    public int MinWeatherIntervalMinutes { get; set; } = 60;
    public int WeatherDifficulty { get; set; } = 1;

    // Child collections (EF can cascade delete these when ScenarioConfig is deleted)
    public List<Flight> Flights { get; set; } = new();
    public List<WeatherInterval> WeatherIntervals { get; set; } = new();
    public List<Aircraft> Aircrafts { get; set; } = new();
}