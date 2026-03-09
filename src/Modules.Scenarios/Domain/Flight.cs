namespace Modules.Scenarios.Domain;

public sealed class Flight
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ScenarioConfigId { get; set; } = Guid.Empty;
    public Guid AircraftId { get; set; } = Guid.Empty;
    //public Guid OriginAirportId { get; set; } = Guid.Empty;
    // public Guid DestinationAirportId { get; set; } = Guid.Empty;

    public string Callsign { get; set; } = string.Empty;

    public FlightType Type { get; set; } = FlightType.Arrival;

    public DateTime ScheduledTime { get; set; }

    // Constraints (minutes)
    public int MaxDelayMinutes { get; set; } = 0;
    public int MaxEarlyMinutes { get; set; } = 0;
    public int Priority { get; set; } = 1;
}
