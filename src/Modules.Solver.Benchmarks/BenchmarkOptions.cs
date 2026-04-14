namespace Modules.Solver.Benchmarks;

public sealed record BenchmarkOptions(
    string OutputPath,
    Guid? ScenarioId,
    string? ScenarioName,
    bool ListScenarios,
    string? ConnectionStringOverride)
{
    public static BenchmarkOptions Parse(string[] args)
    {
        string? outputPath = null;
        Guid? scenarioId = null;
        string? scenarioName = null;
        string? connectionString = null;
        var listScenarios = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--output" when i + 1 < args.Length:
                    outputPath = Path.GetFullPath(args[++i]);
                    break;
                case "--scenario-id" when i + 1 < args.Length && Guid.TryParse(args[++i], out var parsedScenarioId):
                    scenarioId = parsedScenarioId;
                    break;
                case "--scenario-name" when i + 1 < args.Length:
                    scenarioName = args[++i];
                    break;
                case "--connection-string" when i + 1 < args.Length:
                    connectionString = args[++i];
                    break;
                case "--list-scenarios":
                    listScenarios = true;
                    break;
            }
        }

        return new BenchmarkOptions(
            outputPath ?? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "benchmark_results.csv")),
            scenarioId,
            scenarioName,
            listScenarios,
            connectionString);
    }
}
