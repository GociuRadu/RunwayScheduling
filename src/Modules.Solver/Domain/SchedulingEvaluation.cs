namespace Modules.Solver.Domain;

public sealed class SchedulingEvaluation
{
    public IReadOnlyList<SolvedFlight> Flights { get; init; } = [];
    public double Fitness { get; init; }
}
