using MediatR;
using Modules.Solver.Application.Scheduling;
using Modules.Solver.Application.Snapshot;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.UseCases.SolveGreedy;

public sealed class SolveGreedyHandler(
    IScenarioSnapshotFactory snapshotFactory,
    ISchedulingEngine engine)
    : IRequestHandler<SolveGreedyQuery, SolverResult>
{
    private const string AlgorithmName = "Greedy";

    public async Task<SolverResult> Handle(SolveGreedyQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await snapshotFactory.CreateAsync(request.ScenarioConfigId, cancellationToken);
        var prepared = PreparedScenario.From(snapshot);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var evaluation = engine.Evaluate(prepared.SortedFlights, prepared);
        sw.Stop();

        return engine.CreateResult(evaluation, request.ScenarioConfigId, AlgorithmName, sw.Elapsed.TotalMilliseconds);
    }
}
