using System.Diagnostics;
using Modules.Solver.Application.PostProcessing;
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

        var solvedFlights = SchedulerDecoder.Decode(orderedFlights, snapshot);

        // Greedy rescheduling: attempt to recover canceled flights using MaxEarlyMinutes window
        solvedFlights = SimpleReschedulingPostProcessor.Apply(solvedFlights, snapshot);

        stopwatch.Stop();
        return SchedulerDecoder.BuildResult(
            solvedFlights,
            snapshot.Flights.Count,
            stopwatch.Elapsed.TotalMilliseconds,
            AlgorithmName,
            snapshot);
    }
}
