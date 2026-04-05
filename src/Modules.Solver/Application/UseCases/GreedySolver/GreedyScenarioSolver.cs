using System.Diagnostics;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GreedySolver;

public sealed class GreedyScenarioSolver : IScenarioSolver
{
    private const string AlgorithmName = "Greedy";

    public SolverResult Solve(ScenarioSnapshot snapshot)
    {
        var stopwatch = Stopwatch.StartNew();

        var orderedFlights = snapshot.Flights
            .OrderBy(f => f.ScheduledTime)
            .ThenByDescending(f => f.Priority)
            .ToList();

        var solvedFlights = GreedySchedulerDecoder.Decode(orderedFlights, snapshot);

        stopwatch.Stop();
        return GreedySchedulerDecoder.BuildResult(
            solvedFlights,
            snapshot.Flights.Count,
            stopwatch.Elapsed.TotalMilliseconds,
            AlgorithmName,
            snapshot);
    }
}
