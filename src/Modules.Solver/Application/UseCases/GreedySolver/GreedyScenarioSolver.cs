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

            // Step 1: find runways that support this flight type
            var compatibleRunways = GetCompatibleRunways(activeRunways, flight.Type);
            if (compatibleRunways.Count == 0)
            {
                solvedFlights.Add(CreateCanceledFlight(flight, processingOrder, CancellationReason.NoCompatibleRunway));
                continue;
            }

            // Step 2: pick the runway that becomes free the soonest
            var (chosenRunway, runwayFreeAt) = compatibleRunways
                .Select(runway => (Runway: runway, FreeAt: runwayAvailability[runway.Name]))
                .MinBy(candidate => candidate.FreeAt);

            // Step 3: the flight can't land before the runway is free AND before its scheduled time
            DateTime assignedTime;
            if (runwayFreeAt > flight.ScheduledTime)
                assignedTime = runwayFreeAt;
            else
                assignedTime = flight.ScheduledTime;

            // Step 4: cancel if the assigned time falls outside the scenario window
            if (!IsWithinScenario(snapshot, assignedTime))
            {
                solvedFlights.Add(CreateCanceledFlight(flight, processingOrder, CancellationReason.OutsideScenarioWindow));
                continue;
            }

            // Step 5: cancel if the resulting delay exceeds the flight's tolerance
            var delayMinutes = (int)Math.Max(0, (assignedTime - flight.ScheduledTime).TotalMinutes);
            if (delayMinutes > flight.MaxDelayMinutes)
            {
                solvedFlights.Add(CreateCanceledFlight(flight, processingOrder, CancellationReason.ExceedsMaxDelay));
                continue;
            }

            var earlyMinutes = (int)Math.Max(0, (flight.ScheduledTime - assignedTime).TotalMinutes);

            // Step 6: cancel if a random event has fully closed the airport (100% impact)
            var activeWeather = GetActiveWeather(snapshot, assignedTime);
            var activeEvent = GetActiveRandomEvent(snapshot, assignedTime);
            if (activeEvent is not null && activeEvent.ImpactPercent >= 100)
            {
                solvedFlights.Add(CreateCanceledFlight(flight, processingOrder, CancellationReason.NoCompatibleRunway));
                continue;
            }

            // Step 7: compute separation and schedule the flight
            var separation = CalculateSeparation(snapshot, activeWeather, activeEvent);
            var status = DetermineFlightStatus(delayMinutes, earlyMinutes);

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
                Status = status,
                CancellationReason = CancellationReason.None,
                AssignedRunway = chosenRunway.Name,
                AssignedTime = assignedTime,
                DelayMinutes = delayMinutes,
                EarlyMinutes = earlyMinutes,
                SeparationAppliedSeconds = (int)separation.TotalSeconds,
                WeatherAtAssignment = activeWeather?.WeatherType,
                AffectedByRandomEvent = activeEvent is not null
            });

            // Mark the runway as busy until separation has passed
            runwayAvailability[chosenRunway.Name] = assignedTime + separation;
        }

        stopwatch.Stop();
        return BuildResult(solvedFlights, orderedFlights.Count, stopwatch.Elapsed.TotalMilliseconds, snapshot);
    }

    private static FlightStatus DetermineFlightStatus(int delayMinutes, int earlyMinutes)
    {
        if (delayMinutes > 0)
            return FlightStatus.Delayed;

        if (earlyMinutes > 0)
            return FlightStatus.Early;

        return FlightStatus.Scheduled;
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

    private static TimeSpan CalculateSeparation(
        ScenarioSnapshot snapshot,
        WeatherInterval? activeWeather,
        RandomEvent? activeEvent)
    {
        var config = snapshot.ScenarioConfig;

        // Base: configured separation scaled by aircraft wake turbulence category
        var baseSeconds = config.BaseSeparationSeconds * (config.WakePercent / 100.0);

        // Weather: each condition adds a multiplier; falls back to scenario-level WeatherPercent
        double weatherMultiplier;
        if (activeWeather is not null)
            weatherMultiplier = GetWeatherMultiplier(activeWeather.WeatherType);
        else
            weatherMultiplier = config.WeatherPercent / 100.0;

        // Random event: ImpactPercent reduces runway capacity, so separation grows inversely.
        // Example: 90% impact → 10% capacity → 10x separation.
        // ImpactPercent=100 is handled earlier (flight canceled before reaching here).
        double eventMultiplier;
        if (activeEvent is not null)
            eventMultiplier = 1.0 / (1.0 - activeEvent.ImpactPercent / 100.0);
        else
            eventMultiplier = 1.0;

        return TimeSpan.FromSeconds(baseSeconds * weatherMultiplier * eventMultiplier);
    }

    private static double GetWeatherMultiplier(WeatherCondition condition) => condition switch
    {
        WeatherCondition.Clear  => 1.00,
        WeatherCondition.Cloud  => 1.10,
        WeatherCondition.Rain   => 1.30,
        WeatherCondition.Snow   => 1.50,
        WeatherCondition.Fog    => 1.75,
        WeatherCondition.Storm  => 2.00,
        _                       => 1.00
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
        var onTimeFlights    = flights.Count(flight => flight.Status == FlightStatus.Scheduled);
        var earlyFlights     = flights.Count(flight => flight.Status == FlightStatus.Early);
        var delayedFlights   = flights.Count(flight => flight.Status == FlightStatus.Delayed);
        var canceledFlights  = flights.Count(flight => flight.Status == FlightStatus.Canceled);

        var totalDelay   = flights.Sum(flight => flight.DelayMinutes);
        var maxDelay     = flights.Count > 0 ? flights.Max(flight => flight.DelayMinutes) : 0;
        var averageDelay = scheduledFlights > 0 ? (double)totalDelay / scheduledFlights : 0.0;

        var scenarioHours = (snapshot.ScenarioConfig.EndTime - snapshot.ScenarioConfig.StartTime).TotalHours;
        var throughput    = scenarioHours > 0 ? scheduledFlights / scenarioHours : 0.0;

        return new SolverResult
        {
            AlgorithmName = AlgorithmName,
            Flights = flights,
            TotalFlights = totalFlights,
            TotalScheduledFlights = scheduledFlights,
            TotalOnTimeFlights = onTimeFlights,
            TotalEarlyFlights = earlyFlights,
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
