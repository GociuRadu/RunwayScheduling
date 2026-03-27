using System.Diagnostics;
using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GreedySolver;

public sealed class GreedyScenarioSolver : IScenarioSolver
{
    private const string AlgorithmName = "Greedy";

    public SolverResult Solve(ScenarioSnapshot snapshot)
    {
        var stopwatch = Stopwatch.StartNew();
        var orderedFlights = OrderFlights(snapshot.Flights);
        var activeRunways = snapshot.Runways.Where(runway => runway.IsActive).ToList();
        var runwayAvailability = activeRunways.ToDictionary(
            runway => runway.Name,
            _ => snapshot.ScenarioConfig.StartTime);

        var solvedFlights = new List<SolvedFlight>(orderedFlights.Count);

        for (var processingOrder = 0; processingOrder < orderedFlights.Count; processingOrder++)
        {
            var flight = orderedFlights[processingOrder];
            var compatibleRunways = GetCompatibleRunways(activeRunways, flight.Type);
            if (compatibleRunways.Count == 0)
            {
                solvedFlights.Add(CreateCanceledFlight(flight, processingOrder, CancellationReason.NoCompatibleRunway));
                continue;
            }

            var (chosenRunway, runwayFreeAt) = compatibleRunways
                .Select(runway => (Runway: runway, FreeAt: runwayAvailability[runway.Name]))
                .MinBy(candidate => candidate.FreeAt);

            var assignedTime = runwayFreeAt > flight.ScheduledTime
                ? runwayFreeAt
                : flight.ScheduledTime;

            if (!IsWithinScenario(snapshot, assignedTime))
            {
                solvedFlights.Add(CreateCanceledFlight(flight, processingOrder, CancellationReason.OutsideScenarioWindow));
                continue;
            }

            var delayMinutes = (int)Math.Max(0, (assignedTime - flight.ScheduledTime).TotalMinutes);
            if (delayMinutes > flight.MaxDelayMinutes)
            {
                solvedFlights.Add(CreateCanceledFlight(flight, processingOrder, CancellationReason.ExceedsMaxDelay));
                continue;
            }

            var activeWeather = GetActiveWeather(snapshot, assignedTime);
            var activeEvent = GetActiveRandomEvent(snapshot, assignedTime);
            var separation = CalculateSeparation(snapshot, activeWeather, activeEvent);

            solvedFlights.Add(new SolvedFlight
            {
                FlightId = flight.Id,
                ScenarioConfigId = flight.ScenarioConfigId,
                AircraftId = flight.AircraftId,
                Callsign = flight.Callsign,
                Type = flight.Type,
                Priority = flight.Priority,
                ProcessingOrder = processingOrder,
                ScheduledTime = flight.ScheduledTime,
                MaxDelayMinutes = flight.MaxDelayMinutes,
                MaxEarlyMinutes = flight.MaxEarlyMinutes,
                Status = delayMinutes > 0 ? FlightStatus.Delayed : FlightStatus.Scheduled,
                CancellationReason = CancellationReason.None,
                AssignedRunway = chosenRunway.Name,
                AssignedTime = assignedTime,
                DelayMinutes = delayMinutes,
                EarlyMinutes = 0,
                SeparationAppliedSeconds = (int)separation.TotalSeconds,
                WeatherAtAssignment = activeWeather?.WeatherType,
                AffectedByRandomEvent = activeEvent is not null
            });

            runwayAvailability[chosenRunway.Name] = assignedTime + separation;
        }

        stopwatch.Stop();
        return BuildResult(solvedFlights, orderedFlights.Count, stopwatch.Elapsed.TotalMilliseconds, snapshot);
    }

    private static List<Flight> OrderFlights(IReadOnlyList<Flight> flights) =>
        flights
            .OrderBy(flight => flight.ScheduledTime)
            .ThenByDescending(flight => flight.Priority)
            .ToList();

    private static List<Runway> GetCompatibleRunways(List<Runway> runways, FlightType flightType) =>
        runways.Where(runway =>
            runway.RunwayType == RunwayType.Both ||
            (flightType == FlightType.Arrival && runway.RunwayType == RunwayType.Landing) ||
            (flightType == FlightType.Departure && runway.RunwayType == RunwayType.Takeoff) ||
            (flightType == FlightType.OnGround && runway.RunwayType == RunwayType.Takeoff)).ToList();

    private static bool IsWithinScenario(ScenarioSnapshot snapshot, DateTime time) =>
        time >= snapshot.ScenarioConfig.StartTime && time <= snapshot.ScenarioConfig.EndTime;

    private static WeatherInterval? GetActiveWeather(ScenarioSnapshot snapshot, DateTime time) =>
        snapshot.WeatherIntervals.FirstOrDefault(interval => time >= interval.StartTime && time <= interval.EndTime);

    private static RandomEvent? GetActiveRandomEvent(ScenarioSnapshot snapshot, DateTime time) =>
        snapshot.RandomEvents.FirstOrDefault(randomEvent => time >= randomEvent.StartTime && time <= randomEvent.EndTime);

    /// <summary>
    /// Base separation = BaseSeparationSeconds × (WakePercent / 100).
    /// Weather multiplier: derived from the active WeatherInterval condition,
    ///   or falls back to ScenarioConfig.WeatherPercent / 100 when no interval is active.
    /// Random-event multiplier: 1 + (ImpactPercent / 100), so ImpactPercent=50 → ×1.5.
    /// </summary>
    private static TimeSpan CalculateSeparation(
        ScenarioSnapshot snapshot,
        WeatherInterval? activeWeather,
        RandomEvent? activeEvent)
    {
        var config = snapshot.ScenarioConfig;
        var baseSeconds = config.BaseSeparationSeconds * (config.WakePercent / 100.0);
        var weatherMultiplier = activeWeather is not null
            ? GetWeatherMultiplier(activeWeather.WeatherType)
            : config.WeatherPercent / 100.0;
        var eventMultiplier = activeEvent is not null
            ? 1.0 + (activeEvent.ImpactPercent / 100.0)
            : 1.0;

        return TimeSpan.FromSeconds(baseSeconds * weatherMultiplier * eventMultiplier);
    }

    private static double GetWeatherMultiplier(WeatherCondition condition) => condition switch
    {
        WeatherCondition.Clear => 1.00,
        WeatherCondition.Cloud => 1.10,
        WeatherCondition.Rain => 1.30,
        WeatherCondition.Snow => 1.50,
        WeatherCondition.Fog => 1.75,
        WeatherCondition.Storm => 2.00,
        _ => 1.00
    };

    private static SolvedFlight CreateCanceledFlight(Flight flight, int processingOrder, CancellationReason reason) =>
        new()
        {
            FlightId = flight.Id,
            ScenarioConfigId = flight.ScenarioConfigId,
            AircraftId = flight.AircraftId,
            Callsign = flight.Callsign,
            Type = flight.Type,
            Priority = flight.Priority,
            ProcessingOrder = processingOrder,
            ScheduledTime = flight.ScheduledTime,
            MaxDelayMinutes = flight.MaxDelayMinutes,
            MaxEarlyMinutes = flight.MaxEarlyMinutes,
            Status = FlightStatus.Canceled,
            CancellationReason = reason,
            AssignedRunway = null,
            AssignedTime = null,
            DelayMinutes = 0,
            EarlyMinutes = 0,
            SeparationAppliedSeconds = 0,
            WeatherAtAssignment = null,
            AffectedByRandomEvent = false
        };

    private static SolverResult BuildResult(
        List<SolvedFlight> flights,
        int totalFlights,
        double solveTimeMs,
        ScenarioSnapshot snapshot)
    {
        var scheduledFlights = flights.Count(flight => flight.Status != FlightStatus.Canceled);
        var onTimeFlights = flights.Count(flight => flight.Status == FlightStatus.Scheduled);
        var delayedFlights = flights.Count(flight => flight.Status == FlightStatus.Delayed);
        var canceledFlights = flights.Count(flight => flight.Status == FlightStatus.Canceled);

        var totalDelay = flights.Sum(flight => flight.DelayMinutes);
        var maxDelay = flights.Count > 0 ? flights.Max(flight => flight.DelayMinutes) : 0;
        var averageDelay = scheduledFlights > 0 ? (double)totalDelay / scheduledFlights : 0.0;

        var scenarioHours = (snapshot.ScenarioConfig.EndTime - snapshot.ScenarioConfig.StartTime).TotalHours;
        var throughput = scenarioHours > 0 ? scheduledFlights / scenarioHours : 0.0;

        return new SolverResult
        {
            AlgorithmName = AlgorithmName,
            Flights = flights,
            TotalFlights = totalFlights,
            TotalScheduledFlights = scheduledFlights,
            TotalOnTimeFlights = onTimeFlights,
            TotalDelayedFlights = delayedFlights,
            TotalCanceledFlights = canceledFlights,
            TotalDelayMinutes = totalDelay,
            AverageDelayMinutes = averageDelay,
            MaxDelayMinutes = maxDelay,
            SolveTimeMs = solveTimeMs,
            ThroughputFlightsPerHour = throughput
        };
    }
}
