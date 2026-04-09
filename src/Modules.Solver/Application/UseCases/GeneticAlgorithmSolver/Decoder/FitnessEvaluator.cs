using Modules.Solver.Domain;

namespace Modules.Solver.Application.GeneticAlgorithmSolver.Decoder;

public sealed record FitnessScore(double Score) : IComparable<FitnessScore>
{
    // EU 261/2004: average compensation threshold = 180 min (3h, medium-haul), used as base penalty per cancelled flight
    private const double CancellationBase = 180.0;
    // Each priority level adds 20% more weight: priority 1 = 1.0x, priority 2 = 1.2x, priority 5 = ~2.07x
    private static double PriorityMultiplier(int priority) => Math.Pow(1.2, priority - 1);

    public static FitnessScore From(IReadOnlyList<(bool canceled, int delayMinutes, int priority)> flights)
    {
        var score = 0.0;
        foreach (var (canceled, delayMinutes, priority) in flights)
        {
            var multiplier = PriorityMultiplier(priority);
            score += canceled
                ? CancellationBase * multiplier
                : delayMinutes * multiplier;
        }
        return new FitnessScore(score);
    }

    // lower is better
    public int CompareTo(FitnessScore? other) => Score.CompareTo(other?.Score ?? double.MaxValue);

    public static bool operator <(FitnessScore a, FitnessScore b) => a.Score < b.Score;
    public static bool operator >(FitnessScore a, FitnessScore b) => a.Score > b.Score;
}

public sealed class FitnessEvaluator
{
    public FitnessScore Evaluate(IReadOnlyList<SolvedFlight> flights)
    {
        var data = flights
            .Select(f => (canceled: f.Status == FlightStatus.Canceled, f.DelayMinutes, f.Priority))
            .ToList();

        return FitnessScore.From(data);
    }
}
