using MediatR;
using Modules.Solver.Application.Scheduling;
using Modules.Solver.Application.Snapshot;
using Modules.Solver.Application.UseCases.SolveGenetic;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.UseCases.GaBenchmark;

public sealed class GaBenchmarkHandler(
    IScenarioSnapshotFactory snapshotFactory,
    ISchedulingEngine engine,
    IBenchmarkEntryStore benchmarkStore)
    : IRequestHandler<GaBenchmarkQuery, GaBenchmarkResult>
{
    public async Task<GaBenchmarkResult> Handle(GaBenchmarkQuery request, CancellationToken cancellationToken)
    {
        if (request.ScenarioConfigIds.Count == 0)
            throw new ArgumentException("At least one scenario must be provided.", nameof(request.ScenarioConfigIds));

        if (request.Configs.Count == 0)
            throw new ArgumentException("At least one config must be provided.", nameof(request.Configs));

        var solver = new GeneticAlgorithmSolver(engine);
        var now = DateTime.UtcNow;
        var perScenario = new List<List<GaBenchmarkEntry>>(request.ScenarioConfigIds.Count);
        var dbEntries = new List<BenchmarkEntry>();

        foreach (var scenarioId in request.ScenarioConfigIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = await snapshotFactory.CreateAsync(scenarioId, cancellationToken);
            var prepared = PreparedScenario.From(snapshot);
            var scenarioEntries = new List<GaBenchmarkEntry>(request.Configs.Count);

            for (var i = 0; i < request.Configs.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var config = ToGaConfig(request.Configs[i], seed: i);
                var result = solver.Solve(prepared, config, scenarioId, out var solveTimeMs);

                scenarioEntries.Add(new GaBenchmarkEntry(scenarioId, i, config, result.Fitness, solveTimeMs));
                dbEntries.Add(ToDbEntry(scenarioId, i, now, config, result.Fitness, solveTimeMs));
            }

            perScenario.Add([.. scenarioEntries.OrderBy(e => e.Fitness)]);
        }

        await benchmarkStore.AddRangeAsync(dbEntries, cancellationToken);

        var maxRank = perScenario.Max(s => s.Count);
        var interleaved = new List<GaBenchmarkEntry>(perScenario.Sum(s => s.Count));

        for (var rank = 0; rank < maxRank; rank++)
            foreach (var scenarioEntries in perScenario)
                if (rank < scenarioEntries.Count)
                    interleaved.Add(scenarioEntries[rank]);

        return new GaBenchmarkResult(interleaved);
    }

    private static GaConfig ToGaConfig(GaConfigParams p, int seed) => new()
    {
        RandomSeed               = seed,
        PopulationSize           = p.PopulationSize,
        MaxGenerations           = p.MaxGenerations,
        CrossoverRate            = p.CrossoverRate,
        MutationRateLocal        = p.MutationRateLocal,
        MutationRateMemetic      = p.MutationRateMemetic,
        TournamentSize           = p.TournamentSize,
        EliteCount               = p.EliteCount,
        NoImprovementGenerations = p.NoImprovementGenerations,
        CpSatTimeLimitMsMicro    = p.CpSatTimeLimitMsMicro,
        CpSatTimeLimitMsMacro    = p.CpSatTimeLimitMsMacro,
        CpSatNeighborhoodSize    = p.CpSatNeighborhoodSize
    };

    private static BenchmarkEntry ToDbEntry(
        Guid scenarioId, int configIndex, DateTime timestamp,
        GaConfig config, double fitness, double solveTimeMs) => new()
    {
        Id                          = Guid.NewGuid(),
        ScenarioConfigId            = scenarioId,
        AlgorithmType               = "GA",
        ConfigIndex                 = configIndex,
        RunTimestampUtc             = timestamp,
        Fitness                     = fitness,
        SolveTimeMs                 = solveTimeMs,
        PopulationSize              = config.PopulationSize,
        MaxGenerations              = config.MaxGenerations,
        CrossoverRate               = config.CrossoverRate,
        MutationRateLocal           = config.MutationRateLocal,
        MutationRateMemetic         = config.MutationRateMemetic,
        TournamentSize              = config.TournamentSize,
        EliteCount                  = config.EliteCount,
        NoImprovementGenerations    = config.NoImprovementGenerations,
        RandomSeed                  = config.RandomSeed,
        EnableCpSatRefinement       = config.EnableCpSatRefinement,
        CpSatMicroEnabled           = config.CpSatMicroEnabled,
        CpSatMicroEveryNGenerations = config.CpSatMicroEveryNGenerations,
        CpSatMacroEnabled           = config.CpSatMacroEnabled,
        CpSatMacroEveryNGenerations = config.CpSatMacroEveryNGenerations,
        CpSatEliteCount             = config.CpSatEliteCount,
        CpSatRandomCount            = config.CpSatRandomCount,
        CpSatMacroWindowCount       = config.CpSatMacroWindowCount,
        CpSatTimeLimitMsMicro       = config.CpSatTimeLimitMsMicro,
        CpSatTimeLimitMsMacro       = config.CpSatTimeLimitMsMacro,
        CpSatNeighborhoodSize       = config.CpSatNeighborhoodSize
    };
}
