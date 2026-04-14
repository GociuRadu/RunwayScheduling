namespace Modules.Solver.Domain;

public sealed class SolverResult
{
    public Guid ScenarioConfigId { get; init; }
    public string AlgorithmName { get; init; } = string.Empty;
    public IReadOnlyList<SolvedFlight> Flights { get; init; } = [];

    public int TotalFlights { get; init; }
    public int TotalScheduledFlights { get; init; }
    public int TotalOnTimeFlights { get; init; }
    public int TotalEarlyFlights { get; init; }
    public int TotalDelayedFlights { get; init; }
    public int TotalCanceledFlights { get; init; }
    public int TotalRescheduledFlights { get; init; }

    public int CanceledNoCompatibleRunway { get; init; }
    public int CanceledOutsideWindow { get; init; }
    public int CanceledExceedsMaxDelay { get; init; }

    public int TotalDelayMinutes { get; init; }
    public double AverageDelayMinutes { get; init; }
    public int MaxDelayMinutes { get; init; }

    public double Fitness { get; init; }
    public double SolveTimeMs { get; init; }
    public double ThroughputFlightsPerHour { get; init; }
}
