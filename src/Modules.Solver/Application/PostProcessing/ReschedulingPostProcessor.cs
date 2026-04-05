using Google.OrTools.Sat;
using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.PostProcessing;

internal static class ReschedulingPostProcessor
{
    // ── Public entry point ─────────────────────────────────────────────────────

    internal static List<SolvedFlight> Apply(
        List<SolvedFlight> flights,
        ScenarioSnapshot   snapshot,
        TimeSpan           budget)
    {
        var canceled = flights.Where(f => f.Status == FlightStatus.Canceled).ToList();
        if (canceled.Count == 0) return flights;

        var frozen     = flights.Where(f => f.Status != FlightStatus.Canceled && f.AssignedTime.HasValue).ToList();
        var candidates = GenerateCandidatesForFlights(canceled, frozen, snapshot);
        if (candidates.Count == 0) return flights;

        var rescheduled = SolveWithCpSat(candidates, canceled, snapshot, budget);
        if (rescheduled.Count == 0) return flights;

        var rescheduledIds = rescheduled.Select(f => f.FlightId).ToHashSet();
        return flights
            .Where(f => !rescheduledIds.Contains(f.FlightId))
            .Concat(rescheduled)
            .ToList();
    }

    // ── Candidate generation ───────────────────────────────────────────────────

    internal static List<SlotCandidate> GenerateCandidatesForFlights(
        IReadOnlyList<SolvedFlight> toReschedule,
        IReadOnlyList<SolvedFlight> frozen,
        ScenarioSnapshot            snapshot)
    {
        var maxScheduledTime = snapshot.Flights.Max(f => f.ScheduledTime);
        var origin           = snapshot.ScenarioConfig.StartTime;
        var scenarioEnd      = snapshot.ScenarioConfig.EndTime;
        var activeRunways    = snapshot.Runways.Where(r => r.IsActive).ToList();

        // Frozen occupancy per runway: (startSec, endSec) sorted by start
        var frozenByRunway = frozen
            .GroupBy(f => f.AssignedRunway!)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f =>
                {
                    var s = (int)(f.AssignedTime!.Value - origin).TotalSeconds;
                    return (Start: s, End: s + f.SeparationAppliedSeconds);
                }).OrderBy(x => x.Start).ToList()
            );

        var result = new List<SlotCandidate>();

        foreach (var flight in toReschedule)
        {
            var windowStart = flight.ScheduledTime.AddMinutes(-flight.MaxEarlyMinutes);
            if (windowStart < snapshot.ScenarioConfig.StartTime)
                windowStart = snapshot.ScenarioConfig.StartTime;

            var windowEnd = maxScheduledTime < scenarioEnd ? maxScheduledTime : scenarioEnd;
            if (windowEnd <= windowStart) continue;

            var compatibleRunways = SchedulerDecoder.GetCompatibleRunways(activeRunways, flight.Type);

            foreach (var runway in compatibleRunways)
            {
                var blocked = frozenByRunway.TryGetValue(runway.Name, out var b) ? b : [];

                // Candidate starts: window start, after each frozen slot, and scheduled time
                var candidateStarts = new SortedSet<DateTime> { windowStart };

                foreach (var (_, endSec) in blocked)
                {
                    var t = origin.AddSeconds(endSec);
                    if (t >= windowStart && t < windowEnd)
                        candidateStarts.Add(t);
                }

                if (flight.ScheduledTime >= windowStart && flight.ScheduledTime <= windowEnd)
                    candidateStarts.Add(flight.ScheduledTime);

                foreach (var candidateStart in candidateStarts)
                {
                    // Advance past any blocking frozen interval
                    var t = candidateStart;
                    bool moved;
                    do
                    {
                        moved = false;
                        foreach (var (bStart, bEnd) in blocked)
                        {
                            var bS = origin.AddSeconds(bStart);
                            var bE = origin.AddSeconds(bEnd);
                            if (t >= bS && t < bE) { t = bE; moved = true; break; }
                        }
                    } while (moved);

                    if (t >= windowEnd || t > scenarioEnd) continue;

                    // Check random event blocks slot entirely
                    var evt = SchedulerDecoder.GetActiveRandomEvent(snapshot, t);
                    if (evt is not null && evt.ImpactPercent >= 100) continue;

                    var weather = SchedulerDecoder.GetActiveWeather(snapshot, t);
                    var sep     = SchedulerDecoder.CalculateSeparation(snapshot, weather, evt);
                    var slotEnd = t + sep;

                    if (slotEnd > scenarioEnd) continue;

                    var startSec = (int)(t - origin).TotalSeconds;
                    var endSec   = (int)(slotEnd - origin).TotalSeconds;

                    // Full slot must not overlap any frozen interval
                    if (blocked.Any(bi => startSec < bi.End && endSec > bi.Start)) continue;

                    var delayMinutes = (int)Math.Max(0, (t - flight.ScheduledTime).TotalMinutes);
                    var earlyMinutes = (int)Math.Max(0, (flight.ScheduledTime - t).TotalMinutes);

                    result.Add(new SlotCandidate(
                        flight.FlightId,
                        runway.Name,
                        startSec,
                        endSec,
                        delayMinutes,
                        earlyMinutes,
                        flight.Priority,
                        delayMinutes == 0 && earlyMinutes == 0,
                        weather?.WeatherType,
                        evt is not null
                    ));
                }
            }
        }

        return result;
    }

    // ── CP-SAT solver ──────────────────────────────────────────────────────────

    internal static List<SolvedFlight> SolveWithCpSat(
        List<SlotCandidate>         candidates,
        IReadOnlyList<SolvedFlight> toReschedule,
        ScenarioSnapshot            snapshot,
        TimeSpan                    budget)
    {
        var model  = new CpModel();
        var origin = snapshot.ScenarioConfig.StartTime;

        var candidatesByFlight = candidates
            .GroupBy(c => c.FlightId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var x = new Dictionary<SlotCandidate, BoolVar>();

        // Decision variables + at-most-one per flight
        foreach (var flight in toReschedule)
        {
            if (!candidatesByFlight.TryGetValue(flight.FlightId, out var slots) || slots.Count == 0)
                continue;

            var slotVars = new List<BoolVar>(slots.Count);
            foreach (var slot in slots)
            {
                var v = model.NewBoolVar($"x_{slot.FlightId}_{slot.RunwayName}_{slot.StartSec}");
                x[slot] = v;
                slotVars.Add(v);
            }

            model.AddAtMostOne(slotVars);
        }

        if (x.Count == 0) return [];

        // Non-overlap per runway: any two overlapping candidate slots cannot both be chosen
        foreach (var runwayGroup in candidates.GroupBy(c => c.RunwayName))
        {
            var slots = runwayGroup
                .Where(s => x.ContainsKey(s))
                .OrderBy(s => s.StartSec)
                .ToList();

            for (var i = 0; i < slots.Count; i++)
                for (var j = i + 1; j < slots.Count; j++)
                    if (slots[i].StartSec < slots[j].EndSec && slots[j].StartSec < slots[i].EndSec)
                        model.AddAtMostOne(new[] { x[slots[i]], x[slots[j]] });
        }

        // Objective: maximize rescued flights >> on-time bonus >> minimize weighted delay
        var vars   = new List<IntVar>(x.Count);
        var coeffs = new List<long>(x.Count);

        foreach (var (slot, v) in x)
        {
            var penalty = (long)slot.DelayMinutes * (slot.Priority + 1) * 50 + slot.EarlyMinutes * 5;
            var bonus   = slot.IsOnTime ? 5_000L : 0L;
            vars.Add(v);
            coeffs.Add(1_000_000L + bonus - penalty);
        }

        model.Maximize(LinearExpr.WeightedSum([.. vars], [.. coeffs]));

        var solver = new CpSolver
        {
            StringParameters =
                $"max_time_in_seconds:{(int)Math.Ceiling(budget.TotalSeconds)} " +
                "num_search_workers:4 " +
                "random_seed:42"
        };

        var status = solver.Solve(model);
        if (status is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
            return [];

        var result = new List<SolvedFlight>();

        foreach (var (slot, v) in x)
        {
            if (solver.Value(v) != 1) continue;

            var original = toReschedule.First(f => f.FlightId == slot.FlightId);
            var assigned = origin.AddSeconds(slot.StartSec);

            result.Add(new SolvedFlight
            {
                FlightId                 = original.FlightId,
                ScenarioConfigId         = original.ScenarioConfigId,
                AircraftId               = original.AircraftId,
                Callsign                 = original.Callsign,
                Type                     = original.Type,
                Priority                 = original.Priority,
                ProcessingOrder          = original.ProcessingOrder,
                ScheduledTime            = original.ScheduledTime,
                MaxDelayMinutes          = original.MaxDelayMinutes,
                MaxEarlyMinutes          = original.MaxEarlyMinutes,
                Status                   = FlightStatus.Rescheduled,
                CancellationReason       = CancellationReason.None,
                AssignedRunway           = slot.RunwayName,
                AssignedTime             = assigned,
                DelayMinutes             = slot.DelayMinutes,
                EarlyMinutes             = slot.EarlyMinutes,
                SeparationAppliedSeconds = slot.EndSec - slot.StartSec,
                WeatherAtAssignment      = slot.WeatherAtAssignment,
                AffectedByRandomEvent    = slot.AffectedByEvent
            });
        }

        return result;
    }
}
