using System.Diagnostics;
using Modules.Scenarios.Domain;
using Modules.Solver.Application.GeneticAlgorithmSolver.Decoder;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GreedySolver;

public sealed class GreedyScenarioSolver : IScenarioSolver
{
    private const string AlgorithmName = "Greedy";

    private readonly ScheduleDecoder _decoder = new();

    public SolverResult Solve(ScenarioSnapshot snapshot)
    {
        var stopwatch = Stopwatch.StartNew();
        var chromosome = _decoder.BuildGreedyChromosome(snapshot);
        var flights = _decoder.Decode(chromosome, snapshot);
        stopwatch.Stop();

        return SolverResultFactory.Create(
            AlgorithmName,
            flights,
            snapshot.Flights.Count,
            snapshot.ScenarioConfig.StartTime,
            snapshot.ScenarioConfig.EndTime,
            stopwatch.Elapsed.TotalMilliseconds);
    }
}
