using Modules.Solver.Domain;

namespace Modules.Solver.Application;

public interface IScenarioSolver
{
    SolverResult Solve(ScenarioSnapshot snapshot);
}
