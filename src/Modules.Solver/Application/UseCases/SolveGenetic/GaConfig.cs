namespace Modules.Solver.Application.UseCases.SolveGenetic;

public sealed class GaConfig
{
    public int PopulationSize { get; set; } = 80;
    public int MaxGenerations { get; set; } = 250;
    public double CrossoverRate { get; set; } = 0.85;
    public double MutationRateLocal { get; set; } = 0.15;
    public double MutationRateMemetic { get; set; } = 0.20;
    public int TournamentSize { get; set; } = 5;
    public int EliteCount { get; set; } = 2;
    public TimeSpan TimeWindowSize { get; set; } = TimeSpan.FromHours(2);
    public int NoImprovementGenerations { get; set; } = 25;

    public int RandomSeed { get; set; } = 42;

}
