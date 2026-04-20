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
        if (request.ScenarioConfigIds.Count == 0)
            throw new ArgumentException("At least one scenario must be provided.", nameof(request.ScenarioConfigIds));

        if (request.Configs.Count == 0)
            throw new ArgumentException("At least one config must be provided.", nameof(request.Configs));

        var solver = new GeneticAlgorithmSolver(engine);

        // For each scenario, run all configs and sort results by fitness (best = lowest first).
        var perScenario = new List<List<GaBenchmarkEntry>>(request.ScenarioConfigIds.Count);

        foreach (var scenarioId in request.ScenarioConfigIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = await snapshotFactory.CreateAsync(scenarioId, cancellationToken);
            var prepared = PreparedScenario.From(snapshot);

            var scenarioEntries = new List<GaBenchmarkEntry>(request.Configs.Count);

            for (var i = 0; i < request.Configs.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var p = request.Configs[i];
                var config = new GaConfig
                {
                    RandomSeed                = i,
                    PopulationSize            = p.PopulationSize,
                    MaxGenerations            = p.MaxGenerations,
                    CrossoverRate             = p.CrossoverRate,
                    MutationRateLocal         = p.MutationRateLocal,
                    MutationRateMemetic       = p.MutationRateMemetic,
                    TournamentSize            = p.TournamentSize,
                    EliteCount                = p.EliteCount,
                    NoImprovementGenerations  = p.NoImprovementGenerations,
                    CpSatTimeLimitMsMicro     = p.CpSatTimeLimitMsMicro,
                    CpSatTimeLimitMsMacro     = p.CpSatTimeLimitMsMacro,
                    CpSatNeighborhoodSize     = p.CpSatNeighborhoodSize
                };

                var result = solver.Solve(prepared, config, scenarioId, out var solveTimeMs);
                scenarioEntries.Add(new GaBenchmarkEntry(scenarioId, i, config, result.Fitness, solveTimeMs));
            }

            perScenario.Add([.. scenarioEntries.OrderBy(e => e.Fitness)]);
        }

        // Interleave: rank 0 of each scenario, then rank 1, etc.
        var maxRank = perScenario.Max(s => s.Count);
        var interleaved = new List<GaBenchmarkEntry>(perScenario.Sum(s => s.Count));

        for (var rank = 0; rank < maxRank; rank++)
            foreach (var scenarioEntries in perScenario)
                if (rank < scenarioEntries.Count)
                    interleaved.Add(scenarioEntries[rank]);

        WriteCsv(interleaved);
        return new GaBenchmarkResult(interleaved);
    }

    private static void WriteCsv(IReadOnlyList<GaBenchmarkEntry> entries)
    {
        var lines = new List<string>(entries.Count + 1)
        {
            "ScenarioConfigId,ConfigIndex,Fitness,SolveTimeMs,PopulationSize,MaxGenerations,CrossoverRate," +
            "MutationRateLocal,MutationRateMemetic,TournamentSize,EliteCount,NoImprovementGenerations," +
            "CpSatTimeLimitMsMicro,CpSatTimeLimitMsMacro,CpSatNeighborhoodSize"
        };

        foreach (var e in entries)
        {
            lines.Add(string.Join(',',
                e.ScenarioConfigId.ToString(),
                e.ConfigIndex.ToString(CultureInfo.InvariantCulture),
                e.Fitness.ToString(CultureInfo.InvariantCulture),
                e.SolveTimeMs.ToString(CultureInfo.InvariantCulture),
                e.Config.PopulationSize.ToString(CultureInfo.InvariantCulture),
                e.Config.MaxGenerations.ToString(CultureInfo.InvariantCulture),
                e.Config.CrossoverRate.ToString(CultureInfo.InvariantCulture),
                e.Config.MutationRateLocal.ToString(CultureInfo.InvariantCulture),
                e.Config.MutationRateMemetic.ToString(CultureInfo.InvariantCulture),
                e.Config.TournamentSize.ToString(CultureInfo.InvariantCulture),
                e.Config.EliteCount.ToString(CultureInfo.InvariantCulture),
                e.Config.NoImprovementGenerations.ToString(CultureInfo.InvariantCulture),
                e.Config.CpSatTimeLimitMsMicro.ToString(CultureInfo.InvariantCulture),
                e.Config.CpSatTimeLimitMsMacro.ToString(CultureInfo.InvariantCulture),
                e.Config.CpSatNeighborhoodSize.ToString(CultureInfo.InvariantCulture)));
        }

        var outputPath = Path.Combine(Environment.CurrentDirectory, "benchmark_results.csv");
        File.WriteAllLines(outputPath, lines);
    }
}
