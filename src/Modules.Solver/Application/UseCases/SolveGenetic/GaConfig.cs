namespace Modules.Solver.Application.UseCases.SolveGenetic;

public sealed class GaConfig
{
    public int PopulationSize { get; set; } = 80;
    public int MaxGenerations { get; set; } = 600;
    public double CrossoverRate { get; set; } = 0.97;
    public double MutationRateLocal { get; set; } = 0.005;
    public double MutationRateMemetic { get; set; } = 0.01;
    public int TournamentSize { get; set; } = 3;
    public int EliteCount { get; set; } = 6;
    public TimeSpan TimeWindowSize { get; set; } = TimeSpan.FromHours(2);
    public int NoImprovementGenerations { get; set; } = 40;
    public int RandomSeed { get; set; } = 6;

    public bool EnableCpSatRefinement { get; set; } = true;

    public bool CpSatMicroEnabled { get; set; } = true;
    public int CpSatMicroEveryNGenerations { get; set; } = 5;

    public bool CpSatMacroEnabled { get; set; } = true;
    public int CpSatMacroEveryNGenerations { get; set; } = 15;

    public int CpSatEliteCount { get; set; } = 2;

    public int CpSatRandomCount { get; set; } = 2;

    public int CpSatMacroWindowCount { get; set; } = 3;

    public int CpSatTimeLimitMsMicro { get; set; } = 60;

    public int CpSatTimeLimitMsMacro { get; set; } = 150;

    public int CpSatNeighborhoodSize { get; set; } = 8;
}
