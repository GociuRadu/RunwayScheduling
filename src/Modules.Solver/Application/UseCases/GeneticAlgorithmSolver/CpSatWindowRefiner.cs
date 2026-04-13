using Google.OrTools.Sat;
using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GeneticAlgorithmSolver;

public sealed class CpSatWindowRefiner(ScenarioSnapshot snapshot)
{
    private const int MaxWindowHours   = 3;
    private const int CpSatTimeLimitMs = 75;

    // called by the GA loop on elite chromosomes
    // returns an improved assignment for the same flights
    public IReadOnlyList<SolvedFlight> Refine(IReadOnlyList<SolvedFlight> current)
    {
        var windows      = GetWindows();
        var runwayBounds = snapshot.Runways
            .Where(r => r.IsActive)
            .ToDictionary(r => r.Name, _ => snapshot.ScenarioConfig.StartTime);
        var result = new List<SolvedFlight>(current.Count);

        foreach (var window in windows)
            result.AddRange(RefineWindow(window, runwayBounds, current));

        return result;
    }

    // divides [StartTime, EndTime] into ceil(totalHours / MaxWindowHours) equal slices
    // e.g. 24h -> 12 windows, 38.25h -> 20 windows, 1.5h -> 1 window
    private List<List<Flight>> GetWindows()
    {
        if (snapshot.Flights.Count == 0)
            return [];

        var start        = snapshot.ScenarioConfig.StartTime;
        var totalHours   = (snapshot.ScenarioConfig.EndTime - start).TotalHours;
        var windowCount  = Math.Max(1, (int)Math.Ceiling(totalHours / MaxWindowHours));
        var totalSeconds = (snapshot.ScenarioConfig.EndTime - start).TotalSeconds;
        var sliceSeconds = totalSeconds / windowCount;

        var windows = Enumerable.Range(0, windowCount)
            .Select(_ => new List<Flight>())
            .ToList();

        foreach (var flight in snapshot.Flights)
        {
            var offset = (flight.ScheduledTime - start).TotalSeconds;
            var index  = (int)Math.Min(offset / sliceSeconds, windowCount - 1);
            windows[index].Add(flight);
        }

        return windows;
    }

    private IReadOnlyList<SolvedFlight> RefineWindow(
        List<Flight> window,
        Dictionary<string, DateTime> runwayBounds,
        IReadOnlyList<SolvedFlight> current)
    {
        if (window.Count == 0)
            return [];

        var model         = new CpModel();
        var epoch         = snapshot.ScenarioConfig.StartTime;
        var activeRunways = snapshot.Runways.Where(r => r.IsActive).ToList();
        var runwayItvs    = activeRunways.ToDictionary(r => r.Name, _ => new List<IntervalVar>());

        var startVars   = new Dictionary<Guid, IntVar>();
        var presentVars = new Dictionary<Guid, BoolVar>();
        var assignedTo  = new Dictionary<Guid, Dictionary<string, BoolVar>>();

        foreach (var flight in window)
        {
            var schedSec    = (long)(flight.ScheduledTime - epoch).TotalSeconds;
            var earliestSec = schedSec - flight.MaxEarlyMinutes * 60L;
            var latestSec   = schedSec + flight.MaxDelayMinutes * 60L;
            var sepSec      = (long)EstimateSeparation(flight).TotalSeconds;
            var required    = flight.Type == FlightType.Arrival ? RunwayType.Landing : RunwayType.Takeoff;
            var compatible  = activeRunways
                .Where(r => r.RunwayType == RunwayType.Both || r.RunwayType == required)
                .ToList();

            var startVar   = model.NewIntVar(earliestSec, latestSec, $"s_{flight.Id}");
            var presentVar = model.NewBoolVar($"p_{flight.Id}");
            startVars[flight.Id]   = startVar;
            presentVars[flight.Id] = presentVar;
            assignedTo[flight.Id]  = [];

            if (compatible.Count == 0)
            {
                model.Add(presentVar == 0);
                model.Add(startVar == schedSec);
                continue;
            }

            // when not present, pin start to scheduled time so delay = 0
            model.Add(startVar == schedSec).OnlyEnforceIf(presentVar.Not());

            var assignedList = new List<BoolVar>();
            foreach (var runway in compatible)
            {
                var lb          = (long)(runwayBounds[runway.Name] - epoch).TotalSeconds;
                var assignedVar = model.NewBoolVar($"a_{flight.Id}_{runway.Name}");
                var endVar      = model.NewIntVar(earliestSec + sepSec, latestSec + sepSec, $"e_{flight.Id}_{runway.Name}");

                model.Add(startVar >= lb).OnlyEnforceIf(assignedVar);
                model.Add(endVar == LinearExpr.Affine(startVar, 1, sepSec));

                var itv = model.NewOptionalIntervalVar(startVar, sepSec, endVar, assignedVar, $"i_{flight.Id}_{runway.Name}");
                runwayItvs[runway.Name].Add(itv);
                assignedTo[flight.Id][runway.Name] = assignedVar;
                assignedList.Add(assignedVar);
            }

            model.Add(LinearExpr.Sum(assignedList) == 1).OnlyEnforceIf(presentVar);
            model.Add(LinearExpr.Sum(assignedList) == 0).OnlyEnforceIf(presentVar.Not());
        }

        foreach (var (_, itvs) in runwayItvs)
            if (itvs.Count > 0)
                model.AddNoOverlap(itvs);

        // objective: minimize delay cost — reward scheduling
        // delay_sec * pScale - present * 10800 * pScale
        // (10800 sec = 180 min = EU 261 cancellation threshold)
        var objVars    = new List<IntVar>();
        var objWeights = new List<long>();

        foreach (var flight in window)
        {
            var schedSec   = (long)(flight.ScheduledTime - epoch).TotalSeconds;
            var pScale     = (long)Math.Round(Math.Pow(1.2, flight.Priority - 1) * 100);
            var startVar   = startVars[flight.Id];
            var presentVar = presentVars[flight.Id];

            var delayVar = model.NewIntVar(0, flight.MaxDelayMinutes * 60L, $"d_{flight.Id}");
            model.AddMaxEquality(delayVar, [model.NewConstant(0L), LinearExpr.Affine(startVar, 1, -schedSec)]);
            objVars.Add(delayVar);
            objWeights.Add(pScale);

            objVars.Add(presentVar);
            objWeights.Add(-10800L * pScale);
        }

        model.Minimize(LinearExpr.WeightedSum(objVars, objWeights));

        var solver = new CpSolver();
        solver.StringParameters = $"max_time_in_seconds:{CpSatTimeLimitMs / 1000.0:F3}";
        var status = solver.Solve(model);

        if (status is not CpSolverStatus.Optimal and not CpSolverStatus.Feasible)
        {
            // fallback: return current solution for flights in this window
            var currentById = current.ToDictionary(sf => sf.FlightId);
            return window
                .Where(f => currentById.ContainsKey(f.Id))
                .Select(f => currentById[f.Id])
                .ToList();
        }

        var result = new List<SolvedFlight>(window.Count);
        foreach (var flight in window)
        {
            if (!solver.BooleanValue(presentVars[flight.Id]))
            {
                result.Add(CanceledFlight(flight, CancellationReason.ExceedsMaxDelay));
                continue;
            }

            var startSec     = solver.Value(startVars[flight.Id]);
            var assignedTime = epoch.AddSeconds(startSec);
            var schedSec     = (long)(flight.ScheduledTime - epoch).TotalSeconds;
            var delaySec     = Math.Max(0L, startSec - schedSec);
            var earlySec     = Math.Max(0L, schedSec - startSec);
            var sepSec       = (long)EstimateSeparation(flight).TotalSeconds;

            var assignedRunway = assignedTo[flight.Id]
                .First(kv => solver.BooleanValue(kv.Value)).Key;

            // propagate runway availability to next window
            var newBound = assignedTime.AddSeconds(sepSec);
            if (newBound > runwayBounds[assignedRunway])
                runwayBounds[assignedRunway] = newBound;

            result.Add(new SolvedFlight
            {
                FlightId                 = flight.Id,
                ScenarioConfigId         = flight.ScenarioConfigId,
                AircraftId               = flight.AircraftId,
                Callsign                 = flight.Callsign,
                Type                     = flight.Type,
                Priority                 = flight.Priority,
                ProcessingOrder          = 0,
                ScheduledTime            = flight.ScheduledTime,
                MaxDelayMinutes          = flight.MaxDelayMinutes,
                MaxEarlyMinutes          = flight.MaxEarlyMinutes,
                Status                   = delaySec > 0 ? FlightStatus.Delayed
                                         : earlySec > 0 ? FlightStatus.Early
                                         : FlightStatus.Scheduled,
                CancellationReason       = CancellationReason.None,
                AssignedRunway           = assignedRunway,
                AssignedTime             = assignedTime,
                DelayMinutes             = (int)(delaySec / 60),
                EarlyMinutes             = (int)(earlySec / 60),
                SeparationAppliedSeconds = (int)sepSec,
                WeatherAtAssignment      = null,
                AffectedByRandomEvent    = false
            });
        }

        return result;
    }

    // same separation logic as ScheduleDecoder.CalculateSeparation
    private TimeSpan EstimateSeparation(Flight flight)
    {
        var config      = snapshot.ScenarioConfig;
        var baseSeconds = config.BaseSeparationSeconds * (config.WakePercent / 100.0);

        var weather = snapshot.WeatherIntervals
            .Where(w => w.StartTime <= flight.ScheduledTime)
            .MaxBy(w => w.StartTime);

        var weatherMultiplier = weather is null
            ? config.WeatherPercent / 100.0
            : weather.WeatherType switch
            {
                WeatherCondition.Clear => 1.00,
                WeatherCondition.Cloud => 1.10,
                WeatherCondition.Rain  => 1.30,
                WeatherCondition.Snow  => 1.50,
                WeatherCondition.Fog   => 1.75,
                WeatherCondition.Storm => 2.00,
                _                      => 1.00
            };

        var randomEvent = snapshot.RandomEvents
            .FirstOrDefault(e => flight.ScheduledTime >= e.StartTime && flight.ScheduledTime < e.EndTime);

        var eventMultiplier = randomEvent is not null && randomEvent.ImpactPercent < 100
            ? 1.0 / (1.0 - randomEvent.ImpactPercent / 100.0)
            : 1.0;

        return TimeSpan.FromSeconds(baseSeconds * weatherMultiplier * eventMultiplier);
    }

    private static SolvedFlight CanceledFlight(Flight flight, CancellationReason reason) => new()
    {
        FlightId                 = flight.Id,
        ScenarioConfigId         = flight.ScenarioConfigId,
        AircraftId               = flight.AircraftId,
        Callsign                 = flight.Callsign,
        Type                     = flight.Type,
        Priority                 = flight.Priority,
        ProcessingOrder          = 0,
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
