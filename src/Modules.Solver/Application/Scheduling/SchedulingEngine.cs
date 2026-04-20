using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.Scheduling;

public sealed class SchedulingEngine : ISchedulingEngine
{
    private static readonly Dictionary<WeatherCondition, double> WeatherMultipliers = new()
    {
        [WeatherCondition.Clear] = 1.0,
        [WeatherCondition.Cloud] = 1.2,
        [WeatherCondition.Rain]  = 1.3,
        [WeatherCondition.Snow]  = 1.5,
        [WeatherCondition.Fog]   = 1.75,
        [WeatherCondition.Storm] = 2.1
    };

    public SchedulingEvaluation Evaluate(
        IReadOnlyList<(Flight Flight, Guid SourceId)> orderedFlights,
        PreparedScenario prepared)
    {
        var scenario = prepared.Snapshot.ScenarioConfig;
        var lastRunwayTime = new Dictionary<string, DateTime>();
        var results = new List<SolvedFlight>(orderedFlights.Count);

        for (int i = 0; i < orderedFlights.Count; i++)
        {
            var (flight, sourceId) = orderedFlights[i];
            var compatibleRunways = prepared.RunwaysByType[flight.Type];

            if (compatibleRunways.Count == 0)
            {
                results.Add(MakeCanceled(flight, sourceId, i, CancellationReason.NoCompatibleRunway));
                continue;
            }

            SolvedFlight? best = null;

            foreach (var runway in compatibleRunways)
            {
                var slot = TryAssign(flight, sourceId, i, runway, lastRunwayTime, prepared, scenario);
                if (slot is null) continue;

                if (best is null || SlotScore(slot) < SlotScore(best))
                    best = slot;
            }

            if (best is null)
            {
                var reason = ResolveReason(flight, compatibleRunways, lastRunwayTime, prepared, scenario);
                results.Add(MakeCanceled(flight, sourceId, i, reason));
            }
            else
            {
                lastRunwayTime[best.AssignedRunway!] = best.AssignedTime!.Value;
                results.Add(best);
            }
        }

        return new SchedulingEvaluation
        {
            Flights = results,
            Fitness = ComputeFitness(results)
        };
    }

    // Fixed cost for any early assignment — makes early undesirable unless it clearly beats a delay.
    private const double EarlyActivationPenalty = 1.0;
    private const double EarlyMinuteWeight = 0.5;

    private static SolvedFlight? TryAssign(
        Flight flight,
        Guid sourceId,
        int order,
        Runway runway,
        Dictionary<string, DateTime> lastRunwayTime,
        PreparedScenario prepared,
        ScenarioConfig scenario)
    {
        var earliestWanted = flight.ScheduledTime.AddMinutes(-flight.MaxEarlyMinutes);
        var latestAllowed  = flight.ScheduledTime.AddMinutes(flight.MaxDelayMinutes);

        var windowStart = Max(earliestWanted, scenario.StartTime);
        var windowEnd   = Min(latestAllowed,  scenario.EndTime);
        if (windowStart > windowEnd) return null;

        // ── Attempt 1: on-time or delayed ────────────────────────────────────────
        var onTimeSlot = TryBuildSlot(
            flight, sourceId, order, runway,
            start: Max(windowStart, flight.ScheduledTime),
            end:   windowEnd,
            lastRunwayTime, prepared, scenario);

        // Perfect on-time → return immediately, no need to try early
        if (onTimeSlot?.Status == FlightStatus.Scheduled)
            return onTimeSlot;

        // ── Attempt 2: early fallback ─────────────────────────────────────────────
        SolvedFlight? earlySlot = null;
        if (windowStart < flight.ScheduledTime)
        {
            earlySlot = TryBuildSlot(
                flight, sourceId, order, runway,
                start: windowStart,
                end:   flight.ScheduledTime.AddSeconds(-1),  // must stay strictly early
                lastRunwayTime, prepared, scenario);
        }

        return BetterSlot(earlySlot, onTimeSlot);
    }

    // Tries to place the flight on the given runway starting at 'start'. Returns null if no valid slot fits in the window.
    private static SolvedFlight? TryBuildSlot(
        Flight flight, Guid sourceId, int order,
        Runway runway,
        DateTime start, DateTime end,
        Dictionary<string, DateTime> lastRunwayTime,
        PreparedScenario prepared, ScenarioConfig scenario)
    {
        if (start > end) return null;

        var candidateTime = start;

        if (lastRunwayTime.TryGetValue(runway.Name, out var lastTime))
        {
            int sep = GetSeparationSeconds(candidateTime, prepared, scenario);
            var afterSep = lastTime.AddSeconds(sep);
            if (afterSep > candidateTime)
                candidateTime = afterSep;
        }

        candidateTime = PushPastBlockingEvents(candidateTime, prepared);

        if (candidateTime > end) return null;

        int delayMinutes = 0, delaySeconds = 0, earlyMinutes = 0, earlySeconds = 0;
        FlightStatus status;

        if (candidateTime < flight.ScheduledTime)
        {
            var diff = (flight.ScheduledTime - candidateTime).TotalSeconds;
            earlySeconds = (int)diff;
            earlyMinutes = earlySeconds / 60;
            status = FlightStatus.Early;
        }
        else if (candidateTime > flight.ScheduledTime)
        {
            var diff = (candidateTime - flight.ScheduledTime).TotalSeconds;
            delaySeconds = (int)diff;
            delayMinutes = delaySeconds / 60;
            status = FlightStatus.Delayed;
        }
        else
        {
            status = FlightStatus.Scheduled;
        }

        return new SolvedFlight
        {
            FlightId              = sourceId,
            ScenarioConfigId      = flight.ScenarioConfigId,
            AircraftId            = flight.AircraftId,
            Callsign              = flight.Callsign,
            Type                  = flight.Type,
            Priority              = flight.Priority,
            ProcessingOrder       = order,
            ScheduledTime         = flight.ScheduledTime,
            MaxDelayMinutes       = flight.MaxDelayMinutes,
            MaxEarlyMinutes       = flight.MaxEarlyMinutes,
            Status                = status,
            CancellationReason    = CancellationReason.None,
            AssignedRunway        = runway.Name,
            AssignedTime          = candidateTime,
            DelayMinutes          = delayMinutes,
            DelaySeconds          = delaySeconds,
            EarlyMinutes          = earlyMinutes,
            EarlySeconds          = earlySeconds,
            SeparationAppliedSeconds = GetSeparationSeconds(candidateTime, prepared, scenario),
            WeatherAtAssignment   = GetWeatherAt(candidateTime, prepared),
            AffectedByRandomEvent = IsAffectedByEvent(candidateTime, prepared)
        };
    }

    // Returns whichever slot has a lower penalty score; null always loses to a real slot.
    private static SolvedFlight? BetterSlot(SolvedFlight? a, SolvedFlight? b)
    {
        if (a is null) return b;
        if (b is null) return a;
        return SlotScore(a) <= SlotScore(b) ? a : b;
    }

    private static int GetSeparationSeconds(DateTime time, PreparedScenario prepared, ScenarioConfig scenario)
    {
        double multiplier = 1.0;

        var weather = GetWeatherAt(time, prepared);
        if (weather.HasValue && WeatherMultipliers.TryGetValue(weather.Value, out var wm))
            multiplier *= wm;

        foreach (var ev in prepared.SortedEvents)
        {
            if (ev.StartTime > time) break;
            // 100%-impact events are handled by PushPastBlockingEvents; skip here
            if (ev.EndTime > time && ev.ImpactPercent > 0 && ev.ImpactPercent < 100)
                multiplier *= 1.0 / (1.0 - ev.ImpactPercent / 100.0);
        }

        return (int)(scenario.BaseSeparationSeconds * multiplier);
    }

    private static WeatherCondition? GetWeatherAt(DateTime time, PreparedScenario prepared)
    {
        foreach (var w in prepared.SortedWeather)
        {
            if (w.StartTime > time) break;
            if (w.EndTime >= time) return w.WeatherType;
        }
        return null;
    }

    private static bool IsAffectedByEvent(DateTime time, PreparedScenario prepared)
    {
        foreach (var ev in prepared.SortedEvents)
        {
            if (ev.StartTime > time) break;
            if (ev.EndTime > time) return true;
        }
        return false;
    }

    // Keeps pushing time forward past any fully-blocking event (ImpactPercent == 100).
    // Loops because one push can land inside another blocking event.
    private static DateTime PushPastBlockingEvents(DateTime time, PreparedScenario prepared)
    {
        bool moved;
        do
        {
            moved = false;
            foreach (var ev in prepared.SortedEvents)
            {
                if (ev.StartTime > time) break;
                if (ev.ImpactPercent >= 100 && ev.EndTime > time)
                {
                    time = ev.EndTime;
                    moved = true;
                }
            }
        } while (moved);
        return time;
    }

    // EU 261/2004: each priority level adds 20% more weight
    private static double PriorityMultiplier(int priority) => Math.Pow(1.2, priority - 1);

    // Lower penalty = better slot (used to pick best runway).
    // Early carries an activation cost so it is only chosen when it clearly beats a delay.
    private static double SlotScore(SolvedFlight f)
    {
        var m = PriorityMultiplier(f.Priority);
        if (f.EarlySeconds > 0)
            return (EarlyActivationPenalty + f.EarlySeconds / 60.0 * EarlyMinuteWeight) * m;
        return f.DelaySeconds / 60.0 * m;
    }

    private static CancellationReason ResolveReason(
        Flight flight,
        IReadOnlyList<Runway> runways,
        Dictionary<string, DateTime> lastRunwayTime,
        PreparedScenario prepared,
        ScenarioConfig scenario)
    {
        var earliestWanted = flight.ScheduledTime.AddMinutes(-flight.MaxEarlyMinutes);
        var latestAllowed  = flight.ScheduledTime.AddMinutes(flight.MaxDelayMinutes);

        if (latestAllowed < scenario.StartTime || earliestWanted > scenario.EndTime)
            return CancellationReason.OutsideScenarioWindow;

        return CancellationReason.ExceedsMaxDelay;
    }

    // EU 261/2004-inspired penalty: lower = better
    // Cancellation base = 180 min (avg compensation threshold, medium-haul)
    private const double CancellationBase = 180.0;

    private static double ComputeFitness(List<SolvedFlight> flights)
    {
        if (flights.Count == 0) return 0;

        double penalty = 0;
        foreach (var f in flights)
        {
            var m = PriorityMultiplier(f.Priority);
            penalty += f.Status == FlightStatus.Canceled
                ? CancellationBase * m
                : f.DelaySeconds / 60.0 * m + f.EarlySeconds / 60.0 * 0.5 * m;
        }

        return penalty;
    }

    public SolverResult CreateResult(
        SchedulingEvaluation evaluation,
        Guid scenarioConfigId,
        string algorithmName,
        double solveTimeMs)
    {
        var flights = evaluation.Flights;
        int total           = flights.Count;
        int canceled        = 0, onTime = 0, early = 0, delayed = 0, rescheduled = 0;
        int canceledNoRunway = 0, canceledOutside = 0, canceledExceeds = 0;
        int totalDelaySeconds = 0, maxDelayMinutes = 0, totalEarlySeconds = 0;

        foreach (var f in flights)
        {
            switch (f.Status)
            {
                case FlightStatus.Canceled:
                    canceled++;
                    switch (f.CancellationReason)
                    {
                        case CancellationReason.NoCompatibleRunway:  canceledNoRunway++; break;
                        case CancellationReason.OutsideScenarioWindow: canceledOutside++; break;
                        case CancellationReason.ExceedsMaxDelay:     canceledExceeds++;  break;
                    }
                    break;
                case FlightStatus.Scheduled:   onTime++;      break;
                case FlightStatus.Early:       early++;       break;
                case FlightStatus.Delayed:     delayed++;     break;
                case FlightStatus.Rescheduled: rescheduled++; break;
            }

            totalDelaySeconds += f.DelaySeconds;
            if (f.DelayMinutes > maxDelayMinutes) maxDelayMinutes = f.DelayMinutes;
            totalEarlySeconds += f.EarlySeconds;
        }

        int scheduled = total - canceled;
        int totalDelayMinutes = totalDelaySeconds / 60;
        int totalEarlyMinutes = totalEarlySeconds / 60;
        double avgDelay = scheduled > 0 ? (double)totalDelayMinutes / scheduled : 0;

        double throughput = 0;
        var assignedTimes = flights
            .Where(f => f.AssignedTime.HasValue)
            .Select(f => f.AssignedTime!.Value)
            .ToList();

        if (assignedTimes.Count > 1)
        {
            var span = (assignedTimes.Max() - assignedTimes.Min()).TotalHours;
            if (span > 0) throughput = scheduled / span;
        }

        return new SolverResult
        {
            ScenarioConfigId          = scenarioConfigId,
            AlgorithmName             = algorithmName,
            Flights                   = flights,
            TotalFlights              = total,
            TotalScheduledFlights     = scheduled,
            TotalOnTimeFlights        = onTime,
            TotalEarlyFlights         = early,
            TotalDelayedFlights       = delayed,
            TotalCanceledFlights      = canceled,
            TotalRescheduledFlights   = rescheduled,
            CanceledNoCompatibleRunway = canceledNoRunway,
            CanceledOutsideWindow     = canceledOutside,
            CanceledExceedsMaxDelay   = canceledExceeds,
            TotalDelayMinutes         = totalDelayMinutes,
            AverageDelayMinutes       = avgDelay,
            MaxDelayMinutes           = maxDelayMinutes,
            TotalEarlyMinutes         = totalEarlyMinutes,
            Fitness                   = evaluation.Fitness,
            SolveTimeMs               = solveTimeMs,
            ThroughputFlightsPerHour  = throughput
        };
    }

    private static SolvedFlight MakeCanceled(Flight flight, Guid sourceId, int order, CancellationReason reason) =>
        new()
        {
            FlightId           = sourceId,
            ScenarioConfigId   = flight.ScenarioConfigId,
            AircraftId         = flight.AircraftId,
            Callsign           = flight.Callsign,
            Type               = flight.Type,
            Priority           = flight.Priority,
            ProcessingOrder    = order,
            ScheduledTime      = flight.ScheduledTime,
            MaxDelayMinutes    = flight.MaxDelayMinutes,
            MaxEarlyMinutes    = flight.MaxEarlyMinutes,
            Status             = FlightStatus.Canceled,
            CancellationReason = reason
        };

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
