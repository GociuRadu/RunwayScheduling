using Modules.Scenarios.Domain;

namespace Modules.Solver.Domain;

public sealed class SolvedFlight
{
    public Guid FlightId { get; init; }
    public Guid ScenarioConfigId { get; init; }
    public Guid AircraftId { get; init; }
    public string Callsign { get; init; } = string.Empty;
    public FlightType Type { get; init; }
    public int Priority { get; init; }

    public int ProcessingOrder { get; init; }

    public DateTime ScheduledTime { get; init; }
    public int MaxDelayMinutes { get; init; }
    public int MaxEarlyMinutes { get; init; }

    public FlightStatus Status { get; init; }
    public CancellationReason CancellationReason { get; init; } = CancellationReason.None;
    public string? AssignedRunway { get; init; }
    public DateTime? AssignedTime { get; init; }
    public int DelayMinutes { get; init; }
    public int EarlyMinutes { get; init; }

    public int SeparationAppliedSeconds { get; init; }
    public WeatherCondition? WeatherAtAssignment { get; init; }
    public bool AffectedByRandomEvent { get; init; }
}
