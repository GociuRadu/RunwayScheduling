using System.Diagnostics;
using MediatR;
using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Application.Scheduling;
using Modules.Solver.Application.UseCases.SolveGenetic;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.UseCases.SolveFromPayload;

public sealed class SolveFromPayloadHandler(ISchedulingEngine engine)
    : IRequestHandler<SolveFromPayloadQuery, SolverResult>
{
    public Task<SolverResult> Handle(SolveFromPayloadQuery request, CancellationToken cancellationToken)
    {
        var scenarioId = Guid.NewGuid();
        var airportId = Guid.NewGuid();

        var airport = new Airport { Name = request.ScenarioConfig.Name };

        var runways = request.Runways.Select(r => new Runway
        {
            AirportId = airportId,
            Name = r.Name,
            IsActive = r.IsActive,
            RunwayType = (RunwayType)r.RunwayType
        }).ToList();

        var scenarioConfig = new ScenarioConfig
        {
            AirportId = airportId,
            Name = request.ScenarioConfig.Name,
            StartTime = request.ScenarioConfig.StartTime,
            EndTime = request.ScenarioConfig.EndTime,
            BaseSeparationSeconds = request.ScenarioConfig.BaseSeparationSeconds
        };

        var flights = request.Flights.Select(f => new Flight
        {
            ScenarioConfigId = scenarioId,
            Callsign = f.Callsign,
            Type = (FlightType)f.Type,
            ScheduledTime = f.ScheduledTime,
            MaxDelayMinutes = f.MaxDelayMinutes,
            MaxEarlyMinutes = f.MaxEarlyMinutes,
            Priority = f.Priority
        }).ToList();

        var weatherIntervals = request.WeatherIntervals.Select(w => new WeatherInterval
        {
            ScenarioConfigId = scenarioId,
            StartTime = w.StartTime,
            EndTime = w.EndTime,
            WeatherType = (WeatherCondition)w.WeatherType
        }).ToList();

        var randomEvents = request.RandomEvents.Select(e => new RandomEvent
        {
            ScenarioConfigId = scenarioId,
            Name = e.Name,
            Description = e.Description,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            ImpactPercent = e.ImpactPercent
        }).ToList();

        var snapshot = new ScenarioSnapshot
        {
            ScenarioConfig = scenarioConfig,
            Airport = airport,
            Runways = runways,
            RunwaySourceIds = runways.Select(r => r.Id).ToList(),
            Flights = flights,
            FlightSourceIds = flights.Select(f => f.Id).ToList(),
            RandomEvents = randomEvents,
            WeatherIntervals = weatherIntervals
        };

        var prepared = PreparedScenario.From(snapshot);

        SolverResult result;
        if (request.Algorithm.Equals("genetic", StringComparison.OrdinalIgnoreCase))
        {
            var solver = new GeneticAlgorithmSolver(engine);
            result = solver.Solve(prepared, new GaConfig(), scenarioId, out _);
        }
        else
        {
            var sw = Stopwatch.StartNew();
            var evaluation = engine.Evaluate(prepared.SortedFlights, prepared);
            sw.Stop();
            result = engine.CreateResult(evaluation, scenarioId, "Greedy", sw.Elapsed.TotalMilliseconds);
        }

        return Task.FromResult(result);
    }
}
