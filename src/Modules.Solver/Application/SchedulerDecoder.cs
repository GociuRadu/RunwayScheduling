using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application;

internal static class SchedulerDecoder
{
    internal static List<SolvedFlight> Decode(
        IReadOnlyList<Flight> orderedFlights,
        ScenarioSnapshot snapshot)
    {
        var activeRunways = snapshot.Runways.Where(r => r.IsActive).ToList();
        var runwayAvailability = activeRunways.ToDictionary(
            r => r.Name,
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
                .Select(r => (Runway: r, FreeAt: runwayAvailability[r.Name]))
                .MinBy(c => c.FreeAt);

            var assignedTime = runwayFreeAt > flight.ScheduledTime ? runwayFreeAt : flight.ScheduledTime;

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

            var earlyMinutes = (int)Math.Max(0, (flight.ScheduledTime - assignedTime).TotalMinutes);

            var activeWeather = GetActiveWeather(snapshot, assignedTime);
            var activeEvent   = GetActiveRandomEvent(snapshot, assignedTime);

            if (activeEvent is not null && activeEvent.ImpactPercent >= 100)
            {
                solvedFlights.Add(CreateCanceledFlight(flight, processingOrder, CancellationReason.NoCompatibleRunway));
                continue;
            }

            var separation = CalculateSeparation(snapshot, activeWeather, activeEvent);
            var status     = DetermineFlightStatus(delayMinutes, earlyMinutes);

            solvedFlights.Add(new SolvedFlight
            {
                FlightId                = flight.Id,
                ScenarioConfigId        = flight.ScenarioConfigId,
                AircraftId              = flight.AircraftId,
                Callsign                = flight.Callsign,
                Type                    = flight.Type,
                Priority                = flight.Priority,
                ProcessingOrder         = processingOrder,
                ScheduledTime           = flight.ScheduledTime,
                MaxDelayMinutes         = flight.MaxDelayMinutes,
                MaxEarlyMinutes         = flight.MaxEarlyMinutes,
                Status                  = status,
                CancellationReason      = CancellationReason.None,
                AssignedRunway          = chosenRunway.Name,
                AssignedTime            = assignedTime,
                DelayMinutes            = delayMinutes,
                EarlyMinutes            = earlyMinutes,
                SeparationAppliedSeconds = (int)separation.TotalSeconds,
                WeatherAtAssignment     = activeWeather?.WeatherType,
                AffectedByRandomEvent   = activeEvent is not null
            });

            runwayAvailability[chosenRunway.Name] = assignedTime + separation;
        }

        return solvedFlights;
    }

    internal static SolverResult BuildResult(
        List<SolvedFlight> flights,
        int totalFlights,
        double solveTimeMs,
        string algorithmName,
        ScenarioSnapshot snapshot)
    {
        var scheduledFlights = flights.Count(f => f.Status != FlightStatus.Canceled);
        var onTimeFlights    = flights.Count(f => f.Status == FlightStatus.Scheduled);
        var earlyFlights     = flights.Count(f => f.Status == FlightStatus.Early);
        var delayedFlights   = flights.Count(f => f.Status == FlightStatus.Delayed);
        var canceledFlights  = flights.Count(f => f.Status == FlightStatus.Canceled);

        var totalDelay   = flights.Sum(f => f.DelayMinutes);
        var maxDelay     = flights.Count > 0 ? flights.Max(f => f.DelayMinutes) : 0;
        var averageDelay = scheduledFlights > 0 ? (double)totalDelay / scheduledFlights : 0.0;

        var scenarioHours = (snapshot.ScenarioConfig.EndTime - snapshot.ScenarioConfig.StartTime).TotalHours;
        var throughput    = scenarioHours > 0 ? scheduledFlights / scenarioHours : 0.0;

        return new SolverResult
        {
            AlgorithmName          = algorithmName,
            Flights                = flights,
            TotalFlights           = totalFlights,
            TotalScheduledFlights  = scheduledFlights,
            TotalOnTimeFlights     = onTimeFlights,
            TotalEarlyFlights      = earlyFlights,
            TotalDelayedFlights    = delayedFlights,
            TotalCanceledFlights   = canceledFlights,
            TotalDelayMinutes      = totalDelay,
            AverageDelayMinutes    = averageDelay,
            MaxDelayMinutes        = maxDelay,
            SolveTimeMs            = solveTimeMs,
            ThroughputFlightsPerHour = throughput
        };
    }

    internal static List<Runway> GetCompatibleRunways(List<Runway> runways, FlightType flightType) =>
        runways.Where(r =>
            r.RunwayType == RunwayType.Both ||
            (flightType == FlightType.Arrival   && r.RunwayType == RunwayType.Landing) ||
            (flightType == FlightType.Departure && r.RunwayType == RunwayType.Takeoff) ||
            (flightType == FlightType.OnGround  && r.RunwayType == RunwayType.Takeoff)).ToList();

    private static bool IsWithinScenario(ScenarioSnapshot snapshot, DateTime time) =>
        time >= snapshot.ScenarioConfig.StartTime && time <= snapshot.ScenarioConfig.EndTime;

    private static WeatherInterval? GetActiveWeather(ScenarioSnapshot snapshot, DateTime time) =>
        snapshot.WeatherIntervals.FirstOrDefault(i => time >= i.StartTime && time <= i.EndTime);

    private static RandomEvent? GetActiveRandomEvent(ScenarioSnapshot snapshot, DateTime time) =>
        snapshot.RandomEvents.FirstOrDefault(e => time >= e.StartTime && time <= e.EndTime);

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
            ? 1.0 / (1.0 - activeEvent.ImpactPercent / 100.0)
            : 1.0;

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

    internal static FlightStatus DetermineFlightStatus(int delayMinutes, int earlyMinutes)
    {
        if (delayMinutes > 0) return FlightStatus.Delayed;
        if (earlyMinutes > 0) return FlightStatus.Early;
        return FlightStatus.Scheduled;
    }

    internal static SolvedFlight CreateCanceledFlight(Flight flight, int processingOrder, CancellationReason reason) =>
        new()
        {
            FlightId                 = flight.Id,
            ScenarioConfigId         = flight.ScenarioConfigId,
            AircraftId               = flight.AircraftId,
            Callsign                 = flight.Callsign,
            Type                     = flight.Type,
            Priority                 = flight.Priority,
            ProcessingOrder          = processingOrder,
            ScheduledTime            = flight.ScheduledTime,
            MaxDelayMinutes          = flight.MaxDelayMinutes,
            MaxEarlyMinutes          = flight.MaxEarlyMinutes,
            Status                   = FlightStatus.Canceled,
            CancellationReason       = reason,
            AssignedRunway           = null,
            AssignedTime             = null,
            DelayMinutes             = 0,
            EarlyMinutes             = 0,
            SeparationAppliedSeconds = 0,
            WeatherAtAssignment      = null,
            AffectedByRandomEvent    = false
        };
}
