namespace Modules.Solver.Application.UseCases.GaBenchmark;

public sealed record GaConfigParams(
    int PopulationSize,
    int MaxGenerations,
    double CrossoverRate,
    double MutationRateLocal,
    double MutationRateMemetic,
    int TournamentSize,
    int EliteCount,
    int NoImprovementGenerations,
    int CpSatTimeLimitMsMicro,
    int CpSatTimeLimitMsMacro,
    int CpSatNeighborhoodSize);
