namespace Modules.Solver.Application.UseCases.SolveGenetic;

public sealed class GaConfig
{
    public int PopulationSize { get; set; } = 96;
    public int MaxGenerations { get; set; } = 250;
    public double CrossoverRate { get; set; } = 0.90;
    public double MutationRateLocal { get; set; } = 0.02;
    public double MutationRateMemetic { get; set; } = 0.08;
    public int TournamentSize { get; set; } = 3;
    public int EliteCount { get; set; } = 4;
    public TimeSpan TimeWindowSize { get; set; } = TimeSpan.FromHours(2);
    public int NoImprovementGenerations { get; set; } = 30;
    public int RandomSeed { get; set; } = 42;

    public bool EnableCpSatRefinement { get; set; } = true;

    /// <summary>Runs a small CP-SAT pass during the GA loop.</summary>
    public bool CpSatMicroEnabled { get; set; } = true;
    public int CpSatMicroEveryNGenerations { get; set; } = 5;

    /// <summary>Runs a deeper CP-SAT pass on the current best chromosome.</summary>
    public bool CpSatMacroEnabled { get; set; } = true;
    public int CpSatMacroEveryNGenerations { get; set; } = 15;

    /// <summary>Top chromosomes refined on each micro pass.</summary>
    public int CpSatEliteCount { get; set; } = 2;

    /// <summary>Random chromosomes refined on each micro pass.</summary>
    public int CpSatRandomCount { get; set; } = 0;

    /// <summary>Worst windows merged for a macro pass.</summary>
    public int CpSatMacroWindowCount { get; set; } = 3;

    /// <summary>Time limit for a micro pass, in ms.</summary>
    public int CpSatTimeLimitMsMicro { get; set; } = 60;

    /// <summary>Time limit for a macro pass, in ms.</summary>
    public int CpSatTimeLimitMsMacro { get; set; } = 150;

    /// <summary>Max flights included in a refinement neighborhood.</summary>
    public int CpSatNeighborhoodSize { get; set; } = 8;
}
