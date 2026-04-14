using MediatR;
using Modules.Solver.Application.Scheduling;
using Modules.Solver.Application.Snapshot;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.UseCases.SolveGenetic;

public sealed class SolveGeneticHandler(
    IScenarioSnapshotFactory snapshotFactory,
    ISchedulingEngine engine)
    : IRequestHandler<SolveGeneticQuery, SolverResult>
{
    public async Task<SolverResult> Handle(SolveGeneticQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await snapshotFactory.CreateAsync(request.ScenarioConfigId, cancellationToken);
        var prepared = PreparedScenario.From(snapshot);

        var solver = new GeneticAlgorithmSolver(engine);
        return solver.Solve(prepared, new GaConfig(), request.ScenarioConfigId, out _);
    }
}
