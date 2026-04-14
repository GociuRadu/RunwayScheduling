using System.Globalization;
using System.Text;
using Modules.Solver.Application.GeneticAlgorithmSolver;
using Modules.Solver.Application.GeneticAlgorithmSolver.Decoder;
using Modules.Solver.Application.GreedySolver;
using Modules.Solver.Domain;

namespace Modules.Solver.Benchmarks;

public static class RunBenchmark
{
    public static int Main(string[] args)
    {
        var options = BenchmarkOptions.Parse(args);
        if (options.ListScenarios)
            return ListScenarios(options);

        var outputPath = options.OutputPath;
        var htmlPath = Path.ChangeExtension(outputPath, ".html");
        var snapshot = ResolveScenarioSnapshot(options, out var scenarioLabel);
        var evaluator = new FitnessEvaluator();
        var configs = BenchmarkConfig.Configs;

        Console.WriteLine($"Running {configs.Count} configurations against scenario '{scenarioLabel}'.");
        Console.WriteLine($"Flights={snapshot.Flights.Count}, ActiveRunways={snapshot.Runways.Count(runway => runway.IsActive)}, Window={(snapshot.ScenarioConfig.EndTime - snapshot.ScenarioConfig.StartTime).TotalHours:F1}h");

        var rows = new List<ResultRow>(configs.Count + 1);

        var greedyResult = new GreedyScenarioSolver().Solve(snapshot);
        var greedyFitness = evaluator.Evaluate(greedyResult.Flights).Score;
        rows.Add(new ResultRow("Greedy", null, greedyResult, greedyFitness));
        Console.WriteLine(
            $"[Greedy] delay={greedyResult.TotalDelayMinutes} canceled={greedyResult.TotalCanceledFlights} " +
            $"fitness={greedyFitness:F2} time={greedyResult.SolveTimeMs:F0}ms");

        for (var i = 0; i < configs.Count; i++)
        {
            var (name, config) = configs[i];
            var solver = new GeneticAlgorithmScenarioSolver(config, new Random(BenchmarkConfig.RandomSeed));
            var result = solver.Solve(snapshot);
            var fitness = evaluator.Evaluate(result.Flights).Score;

            rows.Add(new ResultRow(name, config, result, fitness));

            Console.WriteLine(
                $"[{i + 1}/{configs.Count}] {name} -> " +
                $"delay={result.TotalDelayMinutes} canceled={result.TotalCanceledFlights} " +
                $"fitness={fitness:F2} time={result.SolveTimeMs:F0}ms");
        }

        WriteCsv(outputPath, rows);
        WriteHtml(htmlPath, rows, scenarioLabel);

        Console.WriteLine($"\nCSV  -> {outputPath}");
        Console.WriteLine($"HTML -> {htmlPath}");
        return 0;
    }

    private static int ListScenarios(BenchmarkOptions options)
    {
        var loader = new DbBenchmarkScenarioLoader(options.ConnectionStringOverride);
        var scenarios = loader.ListScenarios();

        if (scenarios.Count == 0)
        {
            Console.WriteLine("No scenarios found in the database.");
            return 1;
        }

        Console.WriteLine("Available scenarios:");
        foreach (var scenario in scenarios)
        {
            Console.WriteLine(
                $"- {scenario.Id} | {scenario.Name} | flights={scenario.FlightCount} | activeRunways={scenario.ActiveRunwayCount} | {scenario.StartTime:u} -> {scenario.EndTime:u}");
        }

        return 0;
    }

    private static ScenarioSnapshot ResolveScenarioSnapshot(BenchmarkOptions options, out string scenarioLabel)
    {
        var loader = new DbBenchmarkScenarioLoader(options.ConnectionStringOverride);
        var snapshot = loader.Load(options.ScenarioId, options.ScenarioName);
        scenarioLabel = $"{snapshot.ScenarioConfig.Name} ({snapshot.ScenarioConfig.Id})";
        return snapshot;
    }

    private static void WriteCsv(string outputPath, IReadOnlyList<ResultRow> rows)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var sb = new StringBuilder();
        sb.AppendLine(
            "Label,PopulationSize,MaxGenerations,ElitismCount,MaxStagnantGenerations," +
            "MutationRate,TournamentSize,RefineEveryNGen,CpSatEliteCount,CpSatTimeLimitMs,MaxWindowHours," +
            "SolveTimeMs,TotalDelayMinutes,TotalCanceledFlights,TotalScheduledFlights,FitnessScore");

        foreach (var row in rows)
        {
            var config = row.Config;
            sb.AppendLine(string.Join(",",
                row.Label,
                config?.PopulationSize.ToString(CultureInfo.InvariantCulture) ?? "-",
                config?.MaxGenerations.ToString(CultureInfo.InvariantCulture) ?? "-",
                config?.ElitismCount.ToString(CultureInfo.InvariantCulture) ?? "-",
                config?.MaxStagnantGenerations.ToString(CultureInfo.InvariantCulture) ?? "-",
                config?.MutationRate.ToString("F4", CultureInfo.InvariantCulture) ?? "-",
                config?.TournamentSize.ToString(CultureInfo.InvariantCulture) ?? "-",
                config?.RefineEveryNGen.ToString(CultureInfo.InvariantCulture) ?? "-",
                config?.CpSatEliteCount.ToString(CultureInfo.InvariantCulture) ?? "-",
                config?.CpSatTimeLimitMs.ToString(CultureInfo.InvariantCulture) ?? "-",
                config?.MaxWindowHours.ToString(CultureInfo.InvariantCulture) ?? "-",
                row.Result.SolveTimeMs.ToString("F2", CultureInfo.InvariantCulture),
                row.Result.TotalDelayMinutes.ToString(CultureInfo.InvariantCulture),
                row.Result.TotalCanceledFlights.ToString(CultureInfo.InvariantCulture),
                row.Result.TotalScheduledFlights.ToString(CultureInfo.InvariantCulture),
                row.Fitness.ToString("F4", CultureInfo.InvariantCulture)));
        }

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    private static void WriteHtml(string outputPath, IReadOnlyList<ResultRow> rows, string scenarioLabel)
    {
        var best = rows.MinBy(row => row.Fitness)!;
        var runDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        sb.AppendLine($$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8">
              <title>GA Benchmark Results</title>
              <style>
                body { font-family: system-ui, sans-serif; background: #0f172a; color: #e2e8f0; padding: 2rem; }
                h1 { font-size: 1.5rem; margin-bottom: .25rem; }
                p.sub { color: #94a3b8; font-size: .875rem; margin-bottom: 1.5rem; }
                .summary { display: flex; gap: 1rem; flex-wrap: wrap; margin-bottom: 1.5rem; }
                .card { background: #1e293b; border-radius: .5rem; padding: 1rem 1.5rem; min-width: 160px; }
                .card .label { font-size: .75rem; color: #64748b; text-transform: uppercase; letter-spacing: .05em; }
                .card .value { font-size: 1.5rem; font-weight: 700; color: #38bdf8; margin-top: .25rem; }
                input[type=search] {
                  background: #1e293b; border: 1px solid #334155; color: #e2e8f0;
                  border-radius: .375rem; padding: .5rem .75rem; font-size: .875rem; width: 320px;
                  margin-bottom: .75rem; outline: none;
                }
                input[type=search]:focus { border-color: #38bdf8; }
                table { width: 100%; border-collapse: collapse; font-size: .8rem; }
                thead { position: sticky; top: 0; z-index: 1; }
                th { background: #1e293b; padding: .6rem .75rem; text-align: left; white-space: nowrap; border-bottom: 1px solid #334155; }
                td { padding: .5rem .75rem; border-bottom: 1px solid #1e293b; }
                tr:hover td { background: #1e293b; }
                tr.best td { background: #0c2a1f !important; }
                tr.greedy td { background: #1a1a2e !important; color: #c4b5fd; }
              </style>
            </head>
            <body>
              <h1>GA Benchmark Results</h1>
              <p class="sub">Scenario: <strong>{{scenarioLabel}}</strong> | {{rows.Count}} runs | {{runDate}}</p>
              <div class="summary">
                <div class="card"><div class="label">Best Fitness</div><div class="value">{{best.Fitness.ToString("F2", CultureInfo.InvariantCulture)}}</div></div>
                <div class="card"><div class="label">Best Config</div><div class="value" style="font-size:1rem">{{best.Label}}</div></div>
                <div class="card"><div class="label">Min Delay</div><div class="value">{{rows.Min(row => row.Result.TotalDelayMinutes)}} min</div></div>
                <div class="card"><div class="label">Min Canceled</div><div class="value">{{rows.Min(row => row.Result.TotalCanceledFlights)}}</div></div>
              </div>
              <input type="search" id="filter" placeholder="Filter rows..." oninput="filterRows()">
              <table id="tbl">
                <thead>
                  <tr>
                    <th>#</th>
                    <th>Pop</th>
                    <th>Gen</th>
                    <th>Mut</th>
                    <th>Tour</th>
                    <th>Refine</th>
                    <th>CP Elite</th>
                    <th>CP ms</th>
                    <th>Window h</th>
                    <th>Delay</th>
                    <th>Canceled</th>
                    <th>Scheduled</th>
                    <th>Fitness</th>
                    <th>Time ms</th>
                  </tr>
                </thead>
                <tbody>
            """);

        foreach (var row in rows)
        {
            var cssClass = row.Label == "Greedy" ? " class=\"greedy\"" : row.Label == best.Label ? " class=\"best\"" : string.Empty;
            var config = row.Config;

            sb.AppendLine(
                $"<tr{cssClass}><td>{row.Label}</td>" +
                $"<td>{config?.PopulationSize.ToString(CultureInfo.InvariantCulture) ?? "-"}</td>" +
                $"<td>{config?.MaxGenerations.ToString(CultureInfo.InvariantCulture) ?? "-"}</td>" +
                $"<td>{config?.MutationRate.ToString("F3", CultureInfo.InvariantCulture) ?? "-"}</td>" +
                $"<td>{config?.TournamentSize.ToString(CultureInfo.InvariantCulture) ?? "-"}</td>" +
                $"<td>{config?.RefineEveryNGen.ToString(CultureInfo.InvariantCulture) ?? "-"}</td>" +
                $"<td>{config?.CpSatEliteCount.ToString(CultureInfo.InvariantCulture) ?? "-"}</td>" +
                $"<td>{config?.CpSatTimeLimitMs.ToString(CultureInfo.InvariantCulture) ?? "-"}</td>" +
                $"<td>{config?.MaxWindowHours.ToString(CultureInfo.InvariantCulture) ?? "-"}</td>" +
                $"<td>{row.Result.TotalDelayMinutes.ToString(CultureInfo.InvariantCulture)}</td>" +
                $"<td>{row.Result.TotalCanceledFlights.ToString(CultureInfo.InvariantCulture)}</td>" +
                $"<td>{row.Result.TotalScheduledFlights.ToString(CultureInfo.InvariantCulture)}</td>" +
                $"<td>{row.Fitness.ToString("F2", CultureInfo.InvariantCulture)}</td>" +
                $"<td>{row.Result.SolveTimeMs.ToString("F0", CultureInfo.InvariantCulture)}</td></tr>");
        }

        sb.AppendLine("""
                </tbody>
              </table>
              <script>
                function filterRows() {
                  const q = document.getElementById('filter').value.toLowerCase();
                  [...document.querySelectorAll('#tbl tbody tr')].forEach(r => {
                    r.style.display = r.innerText.toLowerCase().includes(q) ? '' : 'none';
                  });
                }
              </script>
            </body>
            </html>
            """);

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    private sealed record ResultRow(string Label, GaSolverConfig? Config, SolverResult Result, double Fitness);
}
