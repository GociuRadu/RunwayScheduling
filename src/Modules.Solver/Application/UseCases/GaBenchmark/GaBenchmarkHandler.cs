using System.Globalization;
using MediatR;
using Modules.Solver.Application.Scheduling;
using Modules.Solver.Application.Snapshot;
using Modules.Solver.Application.UseCases.SolveGenetic;

namespace Modules.Solver.Application.UseCases.GaBenchmark;

public sealed class GaBenchmarkHandler(
    IScenarioSnapshotFactory snapshotFactory,
    ISchedulingEngine engine)
    : IRequestHandler<GaBenchmarkQuery, GaBenchmarkResult>
{
    public async Task<GaBenchmarkResult> Handle(GaBenchmarkQuery request, CancellationToken cancellationToken)
    {
        if (request.Runs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Runs), "Runs cannot be negative.");
        }

        var snapshot = await snapshotFactory.CreateAsync(request.ScenarioConfigId, cancellationToken);
        var prepared = PreparedScenario.From(snapshot);
        var solver = new GeneticAlgorithmSolver(engine);

        var entries = new List<GaBenchmarkEntry>(request.Runs);

        for (var runIndex = 0; runIndex < request.Runs; runIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var random = new Random(runIndex);
            var config = new GaConfig
            {
                RandomSeed = runIndex,
                PopulationSize = NextInt(random, request.PopulationSizeMin, request.PopulationSizeMax),
                MaxGenerations = NextInt(random, request.MaxGenerationsMin, request.MaxGenerationsMax),
                CrossoverRate = NextDouble(random, request.CrossoverRateMin, request.CrossoverRateMax),
                MutationRateLocal = NextDouble(random, request.MutationRateLocalMin, request.MutationRateLocalMax),
                MutationRateMemetic = NextDouble(random, request.MutationRateMemeticMin, request.MutationRateMemeticMax),
                TournamentSize = NextInt(random, request.TournamentSizeMin, request.TournamentSizeMax),
                EliteCount = NextInt(random, request.EliteCountMin, request.EliteCountMax),
                NoImprovementGenerations = NextInt(random, request.NoImprovementGenerationsMin, request.NoImprovementGenerationsMax),
                CpSatTimeLimitMsMicro = NextInt(random, request.CpSatTimeLimitMsMicroMin, request.CpSatTimeLimitMsMicroMax),
                CpSatTimeLimitMsMacro = NextInt(random, request.CpSatTimeLimitMsMacroMin, request.CpSatTimeLimitMsMacroMax),
                CpSatNeighborhoodSize = NextInt(random, request.CpSatNeighborhoodSizeMin, request.CpSatNeighborhoodSizeMax)
            };

            var result = solver.Solve(prepared, config, request.ScenarioConfigId, out var solveTimeMs);
            entries.Add(new GaBenchmarkEntry(config, result.Fitness, solveTimeMs, runIndex));
        }

        var orderedEntries = entries
            .OrderBy(entry => entry.Fitness)
            .ToList();

        WriteCsv(orderedEntries);
        return new GaBenchmarkResult(orderedEntries);
    }

    private static void WriteCsv(IReadOnlyList<GaBenchmarkEntry> entries)
    {
        var lines = new List<string>(entries.Count + 1)
        {
            "RunIndex,Fitness,SolveTimeMs,PopulationSize,MaxGenerations,CrossoverRate,MutationRateLocal,MutationRateMemetic,TournamentSize,EliteCount,NoImprovementGenerations,CpSatTimeLimitMsMicro,CpSatTimeLimitMsMacro,CpSatNeighborhoodSize"
        };

        foreach (var entry in entries)
        {
            lines.Add(
                string.Join(
                    ',',
                    entry.RunIndex.ToString(CultureInfo.InvariantCulture),
                    entry.Fitness.ToString(CultureInfo.InvariantCulture),
                    entry.SolveTimeMs.ToString(CultureInfo.InvariantCulture),
                    entry.Config.PopulationSize.ToString(CultureInfo.InvariantCulture),
                    entry.Config.MaxGenerations.ToString(CultureInfo.InvariantCulture),
                    entry.Config.CrossoverRate.ToString(CultureInfo.InvariantCulture),
                    entry.Config.MutationRateLocal.ToString(CultureInfo.InvariantCulture),
                    entry.Config.MutationRateMemetic.ToString(CultureInfo.InvariantCulture),
                    entry.Config.TournamentSize.ToString(CultureInfo.InvariantCulture),
                    entry.Config.EliteCount.ToString(CultureInfo.InvariantCulture),
                    entry.Config.NoImprovementGenerations.ToString(CultureInfo.InvariantCulture),
                    entry.Config.CpSatTimeLimitMsMicro.ToString(CultureInfo.InvariantCulture),
                    entry.Config.CpSatTimeLimitMsMacro.ToString(CultureInfo.InvariantCulture),
                    entry.Config.CpSatNeighborhoodSize.ToString(CultureInfo.InvariantCulture)));
        }

        var outputPath = Path.Combine(Environment.CurrentDirectory, "benchmark_results.csv");
        File.WriteAllLines(outputPath, lines);
    }

    private static int NextInt(Random random, int min, int max)
    {
        if (min > max)
        {
            throw new ArgumentException($"Invalid range: min ({min}) cannot be greater than max ({max}).");
        }

        if (min == max)
        {
            return min;
        }

        return max == int.MaxValue
            ? (int)random.NextInt64(min, (long)max + 1)
            : random.Next(min, max + 1);
    }

    private static double NextDouble(Random random, double min, double max)
    {
        if (min > max)
        {
            throw new ArgumentException($"Invalid range: min ({min}) cannot be greater than max ({max}).");
        }

        if (Math.Abs(max - min) < double.Epsilon)
        {
            return min;
        }

        return min + (random.NextDouble() * (max - min));
    }
}
