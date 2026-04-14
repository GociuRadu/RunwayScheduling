using Api.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Benchmarks;

public sealed class DbBenchmarkScenarioLoader
{
    private readonly string _connectionString;

    public DbBenchmarkScenarioLoader(string? connectionStringOverride = null)
    {
        _connectionString = ResolveConnectionString(connectionStringOverride);
    }

    public IReadOnlyList<BenchmarkScenarioInfo> ListScenarios()
    {
        using var dbContext = CreateDbContext();

        var scenarios = dbContext.ScenarioConfigs
            .AsNoTracking()
            .OrderByDescending(scenario => scenario.StartTime)
            .ToList();

        return scenarios
            .Select(scenario => new BenchmarkScenarioInfo(
                scenario.Id,
                scenario.Name,
                scenario.AirportId,
                scenario.StartTime,
                scenario.EndTime,
                dbContext.Flights.Count(flight => flight.ScenarioConfigId == scenario.Id),
                dbContext.Runways.Count(runway => runway.AirportId == scenario.AirportId && runway.IsActive)))
            .ToList();
    }

    public ScenarioSnapshot Load(Guid? scenarioId, string? scenarioName)
    {
        using var dbContext = CreateDbContext();

        var scenarioConfig = ResolveScenario(dbContext, scenarioId, scenarioName);
        var airport = dbContext.Airports.AsNoTracking().FirstOrDefault(airport => airport.Id == scenarioConfig.AirportId)
            ?? throw new InvalidOperationException($"Airport {scenarioConfig.AirportId} was not found.");

        var runways = dbContext.Runways
            .AsNoTracking()
            .Where(runway => runway.AirportId == scenarioConfig.AirportId)
            .ToList();

        var flights = dbContext.Flights
            .AsNoTracking()
            .Where(flight => flight.ScenarioConfigId == scenarioConfig.Id)
            .OrderBy(flight => flight.ScheduledTime)
            .ThenByDescending(flight => flight.Priority)
            .ToList();

        var weatherIntervals = dbContext.WeatherIntervals
            .AsNoTracking()
            .Where(interval => interval.ScenarioConfigId == scenarioConfig.Id)
            .OrderBy(interval => interval.StartTime)
            .ToList();

        var randomEvents = dbContext.RandomEvents
            .AsNoTracking()
            .Where(randomEvent => randomEvent.ScenarioConfigId == scenarioConfig.Id)
            .OrderBy(randomEvent => randomEvent.StartTime)
            .ToList();

        return new ScenarioSnapshot
        {
            ScenarioConfig = scenarioConfig,
            Airport = airport,
            Runways = runways,
            Flights = flights,
            WeatherIntervals = weatherIntervals,
            RandomEvents = randomEvents
        };
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static string ResolveConnectionString(string? connectionStringOverride)
    {
        if (!string.IsNullOrWhiteSpace(connectionStringOverride))
            return connectionStringOverride;

        var fromEnvironment = Environment.GetEnvironmentVariable("BENCHMARK_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default");

        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            return fromEnvironment;

        var apiProjectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Api"));
        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Could not resolve the database connection string for benchmark.");
    }

    private static ScenarioConfig ResolveScenario(AppDbContext dbContext, Guid? scenarioId, string? scenarioName)
    {
        if (scenarioId is not null)
        {
            return dbContext.ScenarioConfigs
                .AsNoTracking()
                .FirstOrDefault(scenario => scenario.Id == scenarioId.Value)
                ?? throw new InvalidOperationException($"Scenario {scenarioId} was not found.");
        }

        if (!string.IsNullOrWhiteSpace(scenarioName))
        {
            return dbContext.ScenarioConfigs
                .AsNoTracking()
                .Where(scenario => EF.Functions.ILike(scenario.Name, $"%{scenarioName}%"))
                .OrderByDescending(scenario => scenario.StartTime)
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"No scenario matching '{scenarioName}' was found.");
        }

        return dbContext.ScenarioConfigs
            .AsNoTracking()
            .OrderByDescending(scenario => scenario.StartTime)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No scenarios were found in the database.");
    }
}

public sealed record BenchmarkScenarioInfo(
    Guid Id,
    string Name,
    Guid AirportId,
    DateTime StartTime,
    DateTime EndTime,
    int FlightCount,
    int ActiveRunwayCount);
