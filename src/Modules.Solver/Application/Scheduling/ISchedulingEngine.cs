using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.Scheduling;

public interface ISchedulingEngine
{
    SchedulingEvaluation Evaluate(
        IReadOnlyList<(Flight Flight, Guid SourceId)> orderedFlights,
        PreparedScenario prepared);

    SolverResult CreateResult(
        SchedulingEvaluation evaluation,
        Guid scenarioConfigId,
        string algorithmName,
        double solveTimeMs);
}
