using Google.OrTools.Sat;
using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Application.Scheduling;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.UseCases.SolveGenetic;

/// <summary>
/// Refines a subset of flights (neighborhood) within a chromosome using CP-SAT.
/// The refiner respects the runway state at the neighborhood's left boundary,
/// uses exact separation computed from weather/events, and only accepts the
/// result if SchedulingEngine confirms a fitness improvement.
/// </summary>
internal sealed class CpSatWindowRefiner(ISchedulingEngine engine)
{
    private const double CancellationBase = 200.0;

    private static readonly Dictionary<WeatherCondition, double> WeatherMultipliers = new()
    {
        [WeatherCondition.Clear] = 1.0,
        [WeatherCondition.Cloud] = 1.2,
        [WeatherCondition.Rain]  = 1.3,
        [WeatherCondition.Snow]  = 1.5,
        [WeatherCondition.Fog]   = 1.75,
        [WeatherCondition.Storm] = 2.1
    };

    /// <summary>
    /// Attempts to refine a neighborhood of flights in the chromosome using CP-SAT.
    /// Modifies <paramref name="chromosome"/> in-place and returns true if fitness improved.
    /// </summary>
    public bool Refine(
        int[] chromosome,
        PreparedScenario prepared,
        IReadOnlyList<int> neighborhoodFlightIndices,
        SchedulingEvaluation currentEval,
        int timeLimitMs)
    {
        if (neighborhoodFlightIndices.Count == 0) return false;

        // Map flight index -> chromosome position
        var flightIndexToPosition = new Dictionary<int, int>(chromosome.Length);
        for (var pos = 0; pos < chromosome.Length; pos++)
            flightIndexToPosition[chromosome[pos]] = pos;

        // Sorted positions of neighborhood flights in the chromosome
        var neighborhoodPositions = neighborhoodFlightIndices
            .Where(fi => flightIndexToPosition.ContainsKey(fi))
            .Select(fi => flightIndexToPosition[fi])
            .OrderBy(p => p)
            .ToList();

        if (neighborhoodPositions.Count == 0) return false;

        var leftBoundary = neighborhoodPositions[0];
        var scenario = prepared.Snapshot.ScenarioConfig;
        var originTime = scenario.StartTime;
        var scenarioEndSec = (long)(scenario.EndTime - originTime).TotalSeconds;

        // Compute runway last-used time from flights BEFORE the neighborhood boundary
        var lastRunwayTimeSec = new Dictionary<string, long>();
        for (var pos = 0; pos < leftBoundary; pos++)
        {
            var sf = currentEval.Flights[pos];
            if (sf.AssignedRunway is not null && sf.AssignedTime.HasValue)
            {
                var tSec = (long)(sf.AssignedTime.Value - originTime).TotalSeconds;
                if (!lastRunwayTimeSec.TryGetValue(sf.AssignedRunway, out var existing) || tSec > existing)
                    lastRunwayTimeSec[sf.AssignedRunway] = tSec;
            }
        }

        var flightIndices = neighborhoodFlightIndices.ToList();
        var n = flightIndices.Count;
        var flights = flightIndices.Select(fi => prepared.SortedFlights[fi].Flight).ToList();

        // Compute conservative separation for this neighborhood window
        var windowEarliest = flights.Select(f => f.ScheduledTime.AddMinutes(-f.MaxEarlyMinutes)).Min();
        var windowLatest   = flights.Select(f => f.ScheduledTime.AddMinutes(f.MaxDelayMinutes)).Max();
        var separationSec  = ComputeMaxSeparationSeconds(windowEarliest, windowLatest, prepared, scenario);

        // ── Build CP-SAT model ──────────────────────────────────────────────────
        var model  = new CpModel();
        var startVars       = new IntVar[n];
        var isScheduledVars = new BoolVar[n];

        // Optional interval vars per runway for NoOverlap
        var runwayIntervals = new Dictionary<string, List<IntervalVar>>();
        foreach (var rw in prepared.ActiveRunways)
            runwayIntervals[rw.Name] = [];

        // presence[i] = list of (BoolVar, runwayName) for each compatible runway
        var presencePerFlight = new List<(BoolVar var, string runwayName)>[n];

        for (var i = 0; i < n; i++)
        {
            var flight          = flights[i];
            var compatibleRwys  = prepared.RunwaysByType[flight.Type];
            var earliestSec     = Math.Max(0L, (long)(flight.ScheduledTime.AddMinutes(-flight.MaxEarlyMinutes) - originTime).TotalSeconds);
            var latestSec       = Math.Min(scenarioEndSec, (long)(flight.ScheduledTime.AddMinutes(flight.MaxDelayMinutes) - originTime).TotalSeconds);

            isScheduledVars[i]  = model.NewBoolVar($"sched_{i}");
            presencePerFlight[i] = [];

            if (compatibleRwys.Count == 0 || earliestSec > latestSec)
            {
                // Cannot be scheduled
                model.Add(isScheduledVars[i] == 0);
                startVars[i] = model.NewIntVar(0, scenarioEndSec, $"s_{i}");
                continue;
            }

            startVars[i] = model.NewIntVar(earliestSec, latestSec, $"s_{i}");

            for (var r = 0; r < compatibleRwys.Count; r++)
            {
                var rw       = compatibleRwys[r];
                var presence = model.NewBoolVar($"p_{i}_{r}");
                presencePerFlight[i].Add((presence, rw.Name));

                var endVar = model.NewIntVar(
                    earliestSec + separationSec,
                    latestSec   + separationSec,
                    $"end_{i}_{r}");

                var interval = model.NewOptionalIntervalVar(
                    startVars[i], separationSec, endVar, presence, $"iv_{i}_{r}");

                runwayIntervals[rw.Name].Add(interval);
            }

            var allPresence = presencePerFlight[i].Select(p => p.var).Cast<IntVar>().ToArray();
            model.Add(LinearExpr.Sum(allPresence) == 1).OnlyEnforceIf(isScheduledVars[i]);
            model.Add(LinearExpr.Sum(allPresence) == 0).OnlyEnforceIf(isScheduledVars[i].Not());
        }

        // NoOverlap constraint per runway (interval duration = separationSec, so NoOverlap ≡ separation enforced)
        foreach (var (_, intervals) in runwayIntervals)
            if (intervals.Count >= 2)
                model.AddNoOverlap(intervals);

        // Boundary constraint: first flight on each runway must respect last-used time from prefix
        for (var i = 0; i < n; i++)
        {
            foreach (var (presenceVar, rwName) in presencePerFlight[i])
            {
                if (lastRunwayTimeSec.TryGetValue(rwName, out var lastSec))
                {
                    var minStart = lastSec + separationSec;
                    if (minStart > 0)
                        model.Add(startVars[i] >= minStart).OnlyEnforceIf(presenceVar);
                }
            }
        }

        // ── Objective ───────────────────────────────────────────────────────────
        // Scale penalty to integers. Multiply priority multiplier × 100.
        // Use ×2 scaling on delay (vs early) to keep 2:1 ratio without division.
        // cancel_penalty_scaled = CancellationBase * 60 * 2 * pMult100
        var objVars   = new List<IntVar>();
        var objCoeffs = new List<long>();

        for (var i = 0; i < n; i++)
        {
            var flight        = flights[i];
            var pMult100      = (long)Math.Round(Math.Pow(1.2, flight.Priority - 1) * 100);
            var scheduledSec  = (long)(flight.ScheduledTime - originTime).TotalSeconds;
            var cancelScaled  = (long)(CancellationBase * 60 * 2 * pMult100);
            var maxSec        = scenarioEndSec;

            var delaySec  = model.NewIntVar(0, maxSec, $"dl_{i}");
            var earlySec  = model.NewIntVar(0, maxSec, $"er_{i}");
            var cancelVar = model.NewBoolVar($"can_{i}");

            // When scheduled: delaySec and earlySec are tight from below
            model.Add(delaySec >= startVars[i] - scheduledSec).OnlyEnforceIf(isScheduledVars[i]);
            model.Add(earlySec >= scheduledSec - startVars[i]).OnlyEnforceIf(isScheduledVars[i]);
            model.Add(delaySec == 0).OnlyEnforceIf(isScheduledVars[i].Not());
            model.Add(earlySec == 0).OnlyEnforceIf(isScheduledVars[i].Not());

            model.Add(cancelVar == 1).OnlyEnforceIf(isScheduledVars[i].Not());
            model.Add(cancelVar == 0).OnlyEnforceIf(isScheduledVars[i]);

            // cancel penalty as explicit IntVar (required for WeightedSum)
            var cancelPenaltyVar = model.NewIntVar(0, cancelScaled, $"cp_{i}");
            model.Add(cancelPenaltyVar == cancelScaled).OnlyEnforceIf(cancelVar);
            model.Add(cancelPenaltyVar == 0).OnlyEnforceIf(cancelVar.Not());

            objVars.Add(delaySec);        objCoeffs.Add(2 * pMult100);
            objVars.Add(earlySec);        objCoeffs.Add(pMult100);
            objVars.Add(cancelPenaltyVar); objCoeffs.Add(1L);
        }

        model.Minimize(LinearExpr.WeightedSum(objVars.ToArray(), objCoeffs.ToArray()));

        // ── Solve ───────────────────────────────────────────────────────────────
        var solver = new CpSolver();
        solver.StringParameters =
            $"max_time_in_seconds:{timeLimitMs / 1000.0:F3}," +
            "num_search_workers:1," +
            "log_search_progress:false";

        var status = solver.Solve(model);
        if (status != CpSolverStatus.Optimal && status != CpSolverStatus.Feasible)
            return false;

        // ── Reconstruct chromosome ──────────────────────────────────────────────
        // Scheduled flights: sort by CP-SAT assigned start time
        // Cancelled flights: append at end of neighborhood (greedy may recover them)
        var sortedNeighborhood = Enumerable.Range(0, n)
            .OrderBy(i => solver.BooleanValue(isScheduledVars[i]) ? solver.Value(startVars[i]) : long.MaxValue)
            .Select(i => flightIndices[i])
            .ToList();

        var newChromosome = (int[])chromosome.Clone();
        for (var p = 0; p < neighborhoodPositions.Count; p++)
            newChromosome[neighborhoodPositions[p]] = sortedNeighborhood[p];

        // ── Accept only if SchedulingEngine confirms improvement ─────────────────
        var permuted = newChromosome
            .Select(idx => prepared.SortedFlights[idx])
            .ToList();

        var candidateEval = engine.Evaluate(permuted, prepared);
        if (candidateEval.Fitness >= currentEval.Fitness)
            return false;

        Array.Copy(newChromosome, chromosome, chromosome.Length);
        return true;
    }

    private static int ComputeMaxSeparationSeconds(
        DateTime from, DateTime to,
        PreparedScenario prepared,
        ScenarioConfig scenario)
    {
        var maxMult = 1.0;

        foreach (var w in prepared.SortedWeather)
        {
            if (w.StartTime > to) break;
            if (w.EndTime >= from && WeatherMultipliers.TryGetValue(w.WeatherType, out var wm) && wm > maxMult)
                maxMult = wm;
        }

        foreach (var ev in prepared.SortedEvents)
        {
            if (ev.StartTime > to) break;
            if (ev.EndTime >= from && ev.ImpactPercent is > 0 and < 100)
            {
                var em = 1.0 / (1.0 - ev.ImpactPercent / 100.0);
                if (em > maxMult) maxMult = em;
            }
        }

        return (int)(scenario.BaseSeparationSeconds * maxMult);
    }
}
