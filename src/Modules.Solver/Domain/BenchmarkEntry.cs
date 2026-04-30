namespace Modules.Solver.Domain;

public sealed class BenchmarkEntry
{
    public Guid Id { get; set; }
    public Guid ScenarioConfigId { get; set; }
    public string AlgorithmType { get; set; } = string.Empty;
    public int ConfigIndex { get; set; }
    public DateTime RunTimestampUtc { get; set; }
    public double Fitness { get; set; }
    public double SolveTimeMs { get; set; }

    public int? PopulationSize { get; set; }
    public int? MaxGenerations { get; set; }
    public double? CrossoverRate { get; set; }
    public double? MutationRateLocal { get; set; }
    public double? MutationRateMemetic { get; set; }
    public int? TournamentSize { get; set; }
    public int? EliteCount { get; set; }
    public int? NoImprovementGenerations { get; set; }
    public int? RandomSeed { get; set; }
    public bool? EnableCpSatRefinement { get; set; }
    public bool? CpSatMicroEnabled { get; set; }
    public int? CpSatMicroEveryNGenerations { get; set; }
    public bool? CpSatMacroEnabled { get; set; }
    public int? CpSatMacroEveryNGenerations { get; set; }
    public int? CpSatEliteCount { get; set; }
    public int? CpSatRandomCount { get; set; }
    public int? CpSatMacroWindowCount { get; set; }
    public int? CpSatTimeLimitMsMicro { get; set; }
    public int? CpSatTimeLimitMsMacro { get; set; }
    public int? CpSatNeighborhoodSize { get; set; }
}
