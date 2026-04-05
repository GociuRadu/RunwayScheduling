using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.PostProcessing;

/// <summary>
/// Greedy post-processor: iterates canceled flights by priority and attempts
/// to assign each to the earliest free compatible runway slot within
/// [ScheduledTime - MaxEarlyMinutes, max(ScheduledTimes)] ∩ [ScenarioStart, ScenarioEnd].
/// </summary>
internal static class SimpleReschedulingPostProcessor
{
    internal static List<SolvedFlight> Apply(
        List<SolvedFlight> flights,
        ScenarioSnapshot   snapshot)
    {
        var canceled = flights
            .Where(f => f.Status == FlightStatus.Canceled)
            .OrderByDescending(f => f.Priority)
            .ThenBy(f => f.ScheduledTime)
            .ToList();

        if (canceled.Count == 0) return flights;

        var activeRunways    = snapshot.Runways.Where(r => r.IsActive).ToList();
        var origin           = snapshot.ScenarioConfig.StartTime;
        var scenarioEnd      = snapshot.ScenarioConfig.EndTime;
        var maxScheduledTime = snapshot.Flights.Max(f => f.ScheduledTime);
        if (maxScheduledTime > scenarioEnd) maxScheduledTime = scenarioEnd;

        // Occupied intervals per runway: (startSec, endSec) sorted by start
        var occupied = activeRunways.ToDictionary(
            r => r.Name,
            r => flights
                .Where(f => f.AssignedRunway == r.Name && f.AssignedTime.HasValue)
                .Select(f =>
                {
                    var s = f.AssignedTime!.Value;
                    return (Start: s, End: s.AddSeconds(f.SeparationAppliedSeconds));
                })
                .OrderBy(x => x.Start)
                .ToList()
        );

        var rescheduled = new List<SolvedFlight>();

        foreach (var cf in canceled)
        {
            var windowStart = cf.ScheduledTime.AddMinutes(-cf.MaxEarlyMinutes);
            if (windowStart < origin) windowStart = origin;
            if (windowStart >= maxScheduledTime) continue;

            var compatible = SchedulerDecoder.GetCompatibleRunways(activeRunways, cf.Type);

            DateTime? bestTime    = null;
            string?   bestRunway  = null;
            TimeSpan  bestSep     = TimeSpan.Zero;
            WeatherInterval?   bestWeather = null;
            RandomEvent?       bestEvent   = null;

            foreach (var runway in compatible)
            {
                var intervals = occupied[runway.Name];
                var t         = windowStart;

                // Find the earliest free slot in window by advancing past conflicts
                while (t < maxScheduledTime && t <= scenarioEnd)
                {
                    var evt = SchedulerDecoder.GetActiveRandomEvent(snapshot, t);
                    if (evt is not null && evt.ImpactPercent >= 100)
                    {
                        // Skip past event block
                        var blockEnd = snapshot.RandomEvents
                            .Where(e => e.StartTime <= t && e.EndTime > t)
                            .Select(e => e.EndTime)
                            .FirstOrDefault();
                        if (blockEnd == default || blockEnd >= maxScheduledTime) break;
                        t = blockEnd;
                        continue;
                    }

                    var weather = SchedulerDecoder.GetActiveWeather(snapshot, t);
                    var sep     = SchedulerDecoder.CalculateSeparation(snapshot, weather, evt);
                    var slotEnd = t + sep;

                    if (slotEnd > scenarioEnd) break;

                    // Check for overlap with existing occupied intervals
                    bool conflict = false;
                    foreach (var (iStart, iEnd) in intervals)
                    {
                        if (t < iEnd && slotEnd > iStart)
                        {
                            t       = iEnd; // advance past conflict
                            conflict = true;
                            break;
                        }
                    }

                    if (conflict) continue;

                    // Valid slot found on this runway
                    if (bestTime is null || t < bestTime.Value)
                    {
                        bestTime   = t;
                        bestRunway = runway.Name;
                        bestSep    = sep;
                        bestWeather = weather;
                        bestEvent   = evt;
                    }
                    break;
                }
            }

            if (bestTime is null || bestRunway is null) continue;

            var delayMinutes = (int)Math.Max(0, (bestTime.Value - cf.ScheduledTime).TotalMinutes);
            var earlyMinutes = (int)Math.Max(0, (cf.ScheduledTime - bestTime.Value).TotalMinutes);

            rescheduled.Add(new SolvedFlight
            {
                FlightId                 = cf.FlightId,
                ScenarioConfigId         = cf.ScenarioConfigId,
                AircraftId               = cf.AircraftId,
                Callsign                 = cf.Callsign,
                Type                     = cf.Type,
                Priority                 = cf.Priority,
                ProcessingOrder          = cf.ProcessingOrder,
                ScheduledTime            = cf.ScheduledTime,
                MaxDelayMinutes          = cf.MaxDelayMinutes,
                MaxEarlyMinutes          = cf.MaxEarlyMinutes,
                Status                   = FlightStatus.Rescheduled,
                CancellationReason       = CancellationReason.None,
                AssignedRunway           = bestRunway,
                AssignedTime             = bestTime.Value,
                DelayMinutes             = delayMinutes,
                EarlyMinutes             = earlyMinutes,
                SeparationAppliedSeconds = (int)bestSep.TotalSeconds,
                WeatherAtAssignment      = bestWeather?.WeatherType,
                AffectedByRandomEvent    = bestEvent is not null
            });

            // Update occupied intervals for chosen runway
            occupied[bestRunway].Add((Start: bestTime.Value, End: bestTime.Value + bestSep));
            occupied[bestRunway].Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        if (rescheduled.Count == 0) return flights;

        var rescheduledIds = rescheduled.Select(f => f.FlightId).ToHashSet();
        return flights
            .Where(f => !rescheduledIds.Contains(f.FlightId))
            .Concat(rescheduled)
            .ToList();
    }
}
