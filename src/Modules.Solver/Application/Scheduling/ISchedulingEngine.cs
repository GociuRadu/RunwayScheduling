using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.Scheduling;

public interface ISchedulingEngine
{
    // assigns each flight to a runway and returns the flights + fitness score
    SchedulingEvaluation Evaluate(
        IReadOnlyList<(Flight Flight, Guid SourceId)> orderedFlights,
        PreparedScenario prepared);

    // builds the final result with statistics (on-time, delayed, canceled, throughput, etc.)
    SolverResult CreateResult(
        SchedulingEvaluation evaluation,
        Guid scenarioConfigId,
        string algorithmName,
        double solveTimeMs);
}
