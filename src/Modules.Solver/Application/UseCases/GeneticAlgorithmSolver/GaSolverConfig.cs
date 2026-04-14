namespace Modules.Solver.Application.GeneticAlgorithmSolver;

public sealed record GaSolverConfig
{
    public int PopulationSize { get; init; } = 129;
    public int MaxGenerations { get; init; } = 105;
    public int ElitismCount { get; init; } = 5;
    public int MaxStagnantGenerations { get; init; } = 200;
    public double MutationRate { get; init; } = 0.031;
    public int TournamentSize { get; init; } = 7;
    public int RefineEveryNGen { get; init; } = 3;
    public int CpSatEliteCount { get; init; } = 3;
    public int CpSatTimeLimitMs { get; init; } = 142;
    public int MaxWindowHours { get; init; } = 2;

}
