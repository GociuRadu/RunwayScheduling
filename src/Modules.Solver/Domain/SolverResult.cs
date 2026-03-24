namespace Modules.Solver.Domain;

public sealed class SolverResult
{
    public IReadOnlyList<SolvedFlight> Flights { get; init; } = [];
    public int TotalFlights { get; init; }
    public int TotalDelayMinutes { get; init; } = 0;
    public int TotalCanceledFlights { get; init; } = 0;
    public int TotalDelayedFlights { get; init; } = 0;
}