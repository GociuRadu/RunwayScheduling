using MediatR;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GeneticAlgorithmSolver;

public sealed class GeneticAlgorithmSolverHandler : IRequestHandler<GeneticAlgorithmScenarioSolverQuery,SolverResult>
{
    public readonly IScenarioSnapshotLoader _snapshotLoader;
    public readonly GeneticAlgorithmScenarioSolver _geneticAlgorithmScenarioSolver;

    public GeneticAlgorithmSolverHandler(
        IScenarioSnapshotLoader snapshotLoader,
        GeneticAlgorithmScenarioSolver geneticAlgorithmScenarioSolver)
    {
        _snapshotLoader = snapshotLoader;
        _geneticAlgorithmScenarioSolver = geneticAlgorithmScenarioSolver;
    }

    public async Task<SolverResult> Handle(GeneticAlgorithmScenarioSolverQuery request, CancellationToken ct)
    {
        var snapshot = await _snapshotLoader.Load(request.ScenarioConfigId, ct);
        return _geneticAlgorithmScenarioSolver.Solve(snapshot);
    }
}