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

    public int Seed { get; set; } = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);

    public int AircraftCount { get; set; } = 20;
    public int AircraftDifficulty { get; set; } = 1;

    public int OnGroundAircraftCount { get; set; } = 10;
    public int InboundAircraftCount { get; set; } = 10;
    public int RemainingOnGroundAircraftCount { get; set; } = 5;

    public int BaseSeparationSeconds { get; set; } = 45;

    public int WakePercent { get; set; } = 100;
    public int WeatherPercent { get; set; } = 100;

    public int WeatherIntervalCount { get; set; } = 4;
    public int MinWeatherIntervalMinutes { get; set; } = 60;
    public int WeatherDifficulty { get; set; } = 1;

    public List<Flight> Flights { get; set; } = new();
    public List<WeatherInterval> WeatherIntervals { get; set; } = new();
    public List<Aircraft> Aircrafts { get; set; } = new();
}
