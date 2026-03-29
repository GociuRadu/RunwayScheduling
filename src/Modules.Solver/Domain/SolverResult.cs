namespace Modules.Solver.Domain;

public sealed class SolverResult
{
    /// <summary>Name of the algorithm that produced this result (e.g. "Greedy").</summary>
    public string AlgorithmName { get; init; } = string.Empty;

    public IReadOnlyList<SolvedFlight> Flights { get; init; } = [];

    public int TotalFlights { get; init; }

    /// <summary>Flights that received a runway assignment (Scheduled + Delayed).</summary>
    public int TotalScheduledFlights { get; init; }

    /// <summary>Flights assigned with zero delay.</summary>
    public int TotalOnTimeFlights { get; init; }

    public int TotalEarlyFlights { get; init; }
    public int TotalDelayedFlights { get; init; }
    public int TotalCanceledFlights { get; init; }

    public int TotalDelayMinutes { get; init; }
    public double AverageDelayMinutes { get; init; }
    public int MaxDelayMinutes { get; init; }

    /// <summary>Wall-clock time the solver took in milliseconds.</summary>
    public double SolveTimeMs { get; init; }

    /// <summary>Scheduled flights per hour of scenario window.</summary>
    public double ThroughputFlightsPerHour { get; init; }
}
