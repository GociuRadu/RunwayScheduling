using Google.OrTools.Sat;
using Modules.Scenarios.Domain;
using Modules.Solver.Application;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GeneticAlgorithmSolver;

public sealed class CpSatWindowRefiner(ScenarioSnapshot snapshot, GaSolverConfig config)
{
    private readonly int _cpSatTimeLimitMs = config.CpSatTimeLimitMs;
    private readonly int _maxWindowHours = config.MaxWindowHours;

    public IReadOnlyList<SolvedFlight> Refine(IReadOnlyList<SolvedFlight> current)
    {
        var windows = ComputeWindowsFromCurrent(current);
        var runwayBounds = snapshot.Runways
            .Where(runway => runway.IsActive)
            .ToDictionary(runway => runway.Name, _ => snapshot.ScenarioConfig.StartTime);
        var result = new List<SolvedFlight>(current.Count);

        foreach (var window in windows)
            result.AddRange(RefineWindow(window, runwayBounds, current));

        return result;
    }

    private List<List<Flight>> ComputeWindowsFromCurrent(IReadOnlyList<SolvedFlight> current)
    {
        if (current.Count == 0)
            return [];

        var flightById = snapshot.Flights.ToDictionary(f => f.Id);
        var ordered = current
            .OrderBy(sf => sf.AssignedTime ?? sf.ScheduledTime)
            .Select(sf => flightById[sf.FlightId])
            .ToList();

        var scenarioStart = snapshot.ScenarioConfig.StartTime;
        var totalHours = (snapshot.ScenarioConfig.EndTime - scenarioStart).TotalHours;
        var windowCount = Math.Max(1, (int)Math.Ceiling(totalHours / _maxWindowHours));
        var sliceSize = ordered.Count / (double)windowCount;

        var windows = Enumerable.Range(0, windowCount)
            .Select(_ => new List<Flight>())
            .ToList();

        for (int i = 0; i < ordered.Count; i++)
        {
            var index = (int)Math.Min(i / sliceSize, windowCount - 1);
            windows[index].Add(ordered[i]);
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

        var model = new CpModel();
        var epoch = snapshot.ScenarioConfig.StartTime;
        var activeRunways = snapshot.Runways.Where(runway => runway.IsActive).ToList();
        var runwayIntervals = activeRunways.ToDictionary(runway => runway.Name, _ => new List<IntervalVar>());

        var startVars = new Dictionary<Guid, IntVar>();
        var presentVars = new Dictionary<Guid, BoolVar>();
        var assignedTo = new Dictionary<Guid, Dictionary<string, BoolVar>>();

        foreach (var flight in window)
        {
            var scheduledSeconds = (long)(flight.ScheduledTime - epoch).TotalSeconds;
            var earliestSeconds = scheduledSeconds - flight.MaxEarlyMinutes * 60L;
            var latestSeconds = scheduledSeconds + flight.MaxDelayMinutes * 60L;
            var separationSeconds = (long)EstimateSeparation(flight).TotalSeconds;
            var compatibleRunways = SchedulingRules.GetCompatibleRunways(activeRunways, flight.Type).ToList();

            var startVar = model.NewIntVar(earliestSeconds, latestSeconds, $"s_{flight.Id}");
            var presentVar = model.NewBoolVar($"p_{flight.Id}");
            startVars[flight.Id] = startVar;
            presentVars[flight.Id] = presentVar;
            assignedTo[flight.Id] = [];

            if (compatibleRunways.Count == 0)
            {
                model.Add(presentVar == 0);
                model.Add(startVar == scheduledSeconds);
                continue;
            }

            model.Add(startVar == scheduledSeconds).OnlyEnforceIf(presentVar.Not());

            var assignedRunwayVars = new List<BoolVar>();
            foreach (var runway in compatibleRunways)
            {
                var lowerBound = (long)(runwayBounds[runway.Name] - epoch).TotalSeconds;
                var assignedVar = model.NewBoolVar($"a_{flight.Id}_{runway.Name}");
                var endVar = model.NewIntVar(earliestSeconds + separationSeconds, latestSeconds + separationSeconds, $"e_{flight.Id}_{runway.Name}");

                model.Add(startVar >= lowerBound).OnlyEnforceIf(assignedVar);
                model.Add(endVar == LinearExpr.Affine(startVar, 1, separationSeconds));

                var interval = model.NewOptionalIntervalVar(startVar, separationSeconds, endVar, assignedVar, $"i_{flight.Id}_{runway.Name}");
                runwayIntervals[runway.Name].Add(interval);
                assignedTo[flight.Id][runway.Name] = assignedVar;
                assignedRunwayVars.Add(assignedVar);
            }

            model.Add(LinearExpr.Sum(assignedRunwayVars) == 1).OnlyEnforceIf(presentVar);
            model.Add(LinearExpr.Sum(assignedRunwayVars) == 0).OnlyEnforceIf(presentVar.Not());
        }

        foreach (var (_, intervals) in runwayIntervals)
            if (intervals.Count > 0)
                model.AddNoOverlap(intervals);

        var objectiveVars = new List<IntVar>();
        var objectiveWeights = new List<long>();

        foreach (var flight in window)
        {
            var scheduledSeconds = (long)(flight.ScheduledTime - epoch).TotalSeconds;
            var priorityScale = (long)Math.Round(Math.Pow(1.2, flight.Priority - 1) * 100);
            var startVar = startVars[flight.Id];
            var presentVar = presentVars[flight.Id];

            var delayVar = model.NewIntVar(0, flight.MaxDelayMinutes * 60L, $"d_{flight.Id}");
            model.AddMaxEquality(delayVar, [model.NewConstant(0L), LinearExpr.Affine(startVar, 1, -scheduledSeconds)]);

            objectiveVars.Add(delayVar);
            objectiveWeights.Add(priorityScale);
            objectiveVars.Add(presentVar);
            objectiveWeights.Add(-10800L * priorityScale);
        }

        model.Minimize(LinearExpr.WeightedSum(objectiveVars, objectiveWeights));

        var solver = new CpSolver
        {
            StringParameters = $"max_time_in_seconds:{_cpSatTimeLimitMs / 1000.0:F3}"
        };

        var status = solver.Solve(model);
        if (status is not CpSolverStatus.Optimal and not CpSolverStatus.Feasible)
        {
            var currentById = current.ToDictionary(flight => flight.FlightId);

            return window
                .Where(flight => currentById.ContainsKey(flight.Id))
                .Select(flight => currentById[flight.Id])
                .ToList();
        }

        var result = new List<SolvedFlight>(window.Count);
        foreach (var flight in window)
        {
            if (!solver.BooleanValue(presentVars[flight.Id]))
            {
                result.Add(SchedulingRules.CreateCanceledFlight(flight, 0, CancellationReason.ExceedsMaxDelay));
                continue;
            }

            var startSeconds = solver.Value(startVars[flight.Id]);
            var assignedTime = epoch.AddSeconds(startSeconds);
            var scheduledSeconds = (long)(flight.ScheduledTime - epoch).TotalSeconds;
            var delaySeconds = Math.Max(0L, startSeconds - scheduledSeconds);
            var earlySeconds = Math.Max(0L, scheduledSeconds - startSeconds);
            var separationSeconds = (long)EstimateSeparation(flight).TotalSeconds;
            var weather = SchedulingRules.FindWeatherAt(snapshot, assignedTime);
            var randomEvent = SchedulingRules.FindRandomEventAt(snapshot, assignedTime);

            var assignedRunway = assignedTo[flight.Id]
                .First(pair => solver.BooleanValue(pair.Value))
                .Key;

            var nextRunwayAvailability = assignedTime.AddSeconds(separationSeconds);
            if (nextRunwayAvailability > runwayBounds[assignedRunway])
                runwayBounds[assignedRunway] = nextRunwayAvailability;

            result.Add(new SolvedFlight
            {
                FlightId = flight.Id,
                ScenarioConfigId = flight.ScenarioConfigId,
                AircraftId = flight.AircraftId,
                Callsign = flight.Callsign,
                Type = flight.Type,
                Priority = flight.Priority,
                ProcessingOrder = 0,
                ScheduledTime = flight.ScheduledTime,
                MaxDelayMinutes = flight.MaxDelayMinutes,
                MaxEarlyMinutes = flight.MaxEarlyMinutes,
                Status = delaySeconds > 0
                    ? FlightStatus.Delayed
                    : earlySeconds > 0
                        ? FlightStatus.Early
                        : FlightStatus.Scheduled,
                CancellationReason = CancellationReason.None,
                AssignedRunway = assignedRunway,
                AssignedTime = assignedTime,
                DelayMinutes = (int)(delaySeconds / 60),
                EarlyMinutes = (int)(earlySeconds / 60),
                SeparationAppliedSeconds = (int)separationSeconds,
                WeatherAtAssignment = weather?.WeatherType,
                AffectedByRandomEvent = randomEvent is not null
            });
        }

        return result;
    }

    private TimeSpan EstimateSeparation(Flight flight)
    {
        var weather = SchedulingRules.FindWeatherAt(snapshot, flight.ScheduledTime);
        var randomEvent = SchedulingRules.FindRandomEventAt(snapshot, flight.ScheduledTime);
        int? impactPercent = randomEvent is { ImpactPercent: < 100 } ? randomEvent.ImpactPercent : null;

        return SchedulingRules.CalculateSeparation(snapshot.ScenarioConfig, weather, impactPercent);
    }
}
