using System.Diagnostics;
using MediatR;
using Modules.Solver.Application.Scheduling;
using Modules.Solver.Application.Snapshot;
using Modules.Solver.Application.UseCases.SolveGenetic;

namespace Modules.Solver.Application.UseCases.Compare;

public sealed class CompareHandler(
    IScenarioSnapshotFactory snapshotFactory,
    ISchedulingEngine engine)
    : IRequestHandler<CompareQuery, CompareResult>
{
    public async Task<CompareResult> Handle(CompareQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await snapshotFactory.CreateAsync(request.ScenarioConfigId, cancellationToken);
        var prepared = PreparedScenario.From(snapshot);

        var sw = Stopwatch.StartNew();
        var greedyEvaluation = engine.Evaluate(prepared.SortedFlights, prepared);
        sw.Stop();

        var greedyResult = engine.CreateResult(
            greedyEvaluation,
            request.ScenarioConfigId,
            "Greedy",
            sw.Elapsed.TotalMilliseconds);

        var geneticResult = new GeneticAlgorithmSolver(engine)
            .Solve(prepared, new GaConfig(), request.ScenarioConfigId, out _);

        return new CompareResult(greedyResult, geneticResult);
    }
}
