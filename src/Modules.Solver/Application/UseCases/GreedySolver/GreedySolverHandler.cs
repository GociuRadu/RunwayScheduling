using MediatR;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GreedySolver;

public sealed class GreedySolverHandler : IRequestHandler<GreedySolverQuery, SolverResult>
{
    private readonly IScenarioSnapshotLoader _snapshotLoader;
    private readonly GreedyScenarioSolver _greedySolver;

    public GreedySolverHandler(
        IScenarioSnapshotLoader snapshotLoader,
        GreedyScenarioSolver greedySolver)
    {
        _snapshotLoader = snapshotLoader;
        _greedySolver = greedySolver;
    }

    public async Task<SolverResult> Handle(GreedySolverQuery request, CancellationToken ct)
    {
        var snapshot = await _snapshotLoader.Load(request.ScenarioConfigId, ct);
        return _greedySolver.Solve(snapshot);
    }
}