namespace Modules.Solver.Application.UseCases.SolveGenetic;

public sealed class GaConfig
{
    // ── GA core ─────────────────────────────────────────────────────────────────
    public int PopulationSize { get; set; } = 80;
    public int MaxGenerations { get; set; } = 250;
    public double CrossoverRate { get; set; } = 0.85;
    public double MutationRateLocal { get; set; } = 0.15;
    public double MutationRateMemetic { get; set; } = 0.20;
    public int TournamentSize { get; set; } = 5;
    public int EliteCount { get; set; } = 2;
    public TimeSpan TimeWindowSize { get; set; } = TimeSpan.FromHours(2);
    public int NoImprovementGenerations { get; set; } = 20;
    public int RandomSeed { get; set; } = 42;

    // ── CP-SAT refinement ────────────────────────────────────────────────────────
    public bool EnableCpSatRefinement { get; set; } = true;

    /// <summary>Fast refinement: 1 worst window, runs every N generations on top elites + random chromosomes.</summary>
    public bool CpSatMicroEnabled { get; set; } = true;
    public int CpSatMicroEveryNGenerations { get; set; } = 2;

    /// <summary>Deep refinement: union of top N worst windows, runs periodically on best elite only.</summary>
    public bool CpSatMacroEnabled { get; set; } = true;
    public int CpSatMacroEveryNGenerations { get; set; } = 10;

    /// <summary>How many top-ranked chromosomes receive micro-refinement each trigger.</summary>
    public int CpSatEliteCount { get; set; } = 1;

    /// <summary>How many randomly-chosen chromosomes receive micro-refinement each trigger (diversity).</summary>
    public int CpSatRandomCount { get; set; } = 1;

    /// <summary>Number of worst time-windows merged into the macro-refinement neighborhood.</summary>
    public int CpSatMacroWindowCount { get; set; } = 3;

    /// <summary>CP-SAT wall-clock time limit for micro-refinement (ms).</summary>
    public int CpSatTimeLimitMsMicro { get; set; } = 150;

    /// <summary>CP-SAT wall-clock time limit for macro-refinement (ms).</summary>
    public int CpSatTimeLimitMsMacro { get; set; } = 400;

    /// <summary>Maximum number of flights in a micro-refinement neighborhood.</summary>
    public int CpSatNeighborhoodSize { get; set; } = 12;
}
