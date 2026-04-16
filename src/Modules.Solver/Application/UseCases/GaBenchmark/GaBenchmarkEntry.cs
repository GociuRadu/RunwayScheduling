using Modules.Solver.Application.UseCases.SolveGenetic;

namespace Modules.Solver.Application.UseCases.GaBenchmark;

public sealed record GaBenchmarkEntry(GaConfig Config, double Fitness, double SolveTimeMs, int RunIndex);
