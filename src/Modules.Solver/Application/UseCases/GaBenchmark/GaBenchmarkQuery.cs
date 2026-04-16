using MediatR;

namespace Modules.Solver.Application.UseCases.GaBenchmark;

public sealed record GaBenchmarkQuery(
    Guid ScenarioConfigId,
    int Runs,
    int PopulationSizeMin,
    int PopulationSizeMax,
    int MaxGenerationsMin,
    int MaxGenerationsMax,
    double CrossoverRateMin,
    double CrossoverRateMax,
    double MutationRateLocalMin,
    double MutationRateLocalMax,
    double MutationRateMemeticMin,
    double MutationRateMemeticMax,
    int TournamentSizeMin,
    int TournamentSizeMax,
    int EliteCountMin,
    int EliteCountMax,
    int NoImprovementGenerationsMin,
    int NoImprovementGenerationsMax,
    int CpSatTimeLimitMsMicroMin,
    int CpSatTimeLimitMsMicroMax,
    int CpSatTimeLimitMsMacroMax,
    int CpSatTimeLimitMsMacroMin,
    int CpSatNeighborhoodSizeMin,
    int CpSatNeighborhoodSizeMax)
    : IRequest<GaBenchmarkResult>;
