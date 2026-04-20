using Modules.Solver.Application.UseCases.SolveGenetic;

namespace Modules.Solver.Application.UseCases.GaBenchmark;

public sealed record GaBenchmarkEntry(
    Guid ScenarioConfigId,
    int ConfigIndex,
    GaConfig Config,
    double Fitness,
    double SolveTimeMs);
