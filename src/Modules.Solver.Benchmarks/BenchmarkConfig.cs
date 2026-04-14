using Modules.Solver.Application.GeneticAlgorithmSolver;

namespace Modules.Solver.Benchmarks;

public static class BenchmarkConfig
{
    public const int RandomSeed = 20260414;
    public const int ConfigCount = 10;

    /// <summary>
    /// 10 randomly sampled configs, each parameter drawn uniformly within its defined range.
    /// The same seed is used so the grid is reproducible across runs.
    /// </summary>
    public static IReadOnlyList<(string Name, GaSolverConfig Config)> Configs { get; } = BuildGrid();

    private static IReadOnlyList<(string Name, GaSolverConfig Config)> BuildGrid()
    {
        var rng = new Random(RandomSeed);
        var list = new List<(string, GaSolverConfig)>(ConfigCount);

        for (int i = 1; i <= ConfigCount; i++)
        {
            var populationSize = rng.Next(50, 251);             // [50, 250]
            var elitismCount   = rng.Next(2, Math.Min(16, populationSize)); // [2, 15], capped by pop
            var cpSatElite     = rng.Next(0, 6);                // [0, 5]

            list.Add(($"GA-{i:D2}", new GaSolverConfig
            {
                PopulationSize         = populationSize,
                MaxGenerations         = rng.Next(100, 301),    // [100, 300]
                ElitismCount           = elitismCount,
                MaxStagnantGenerations = rng.Next(30, 101),     // [30, 100]
                MutationRate           = Math.Round(rng.NextDouble() * 0.18 + 0.02, 4), // [0.02, 0.20]
                TournamentSize         = rng.Next(2, 8),        // [2, 7]
                RefineEveryNGen        = rng.Next(3, 11),       // [3, 10]
                CpSatEliteCount        = cpSatElite,
                CpSatTimeLimitMs       = cpSatElite > 0 ? rng.Next(30, 201) : 30, // [30, 200]
                MaxWindowHours         = rng.Next(1, 3),        // [1, 2]
            }));
        }

        return list;
    }
}
