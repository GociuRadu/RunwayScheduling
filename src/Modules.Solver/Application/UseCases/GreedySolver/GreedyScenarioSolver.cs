using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GreedySolver;

public sealed class GreedyScenarioSolver : IScenarioSolver
{
    public SolverResult Solve(ScenarioSnapshot snapshot)
    {
        var orderedFlights = OrderFlights(snapshot.Flights);
        var separation = CalculateSeparation(snapshot);
        var canFit = CanFitAllFlights(snapshot, orderedFlights.Count, separation);

        var solvedFlights = new List<SolvedFlight>();
        DateTime? runwayAvailableAt = null;

        foreach (var flight in orderedFlights)
        {
            var assignedTime = ComputeAssignedTime(flight, runwayAvailableAt);

            if (!IsWithinScenario(snapshot, assignedTime))
            {
                solvedFlights.Add(CreateCanceledFlight(flight));
                continue;
            }

            var delayMinutes = ComputeDelay(flight, assignedTime);

            if (IsDelayTooLarge(flight, delayMinutes))
            {
                solvedFlights.Add(CreateCanceledFlight(flight));
                continue;
            }

            var assignedRunway = ChooseRunway(snapshot, flight, assignedTime);

            var solved = CreateSolvedFlight(flight, assignedTime, delayMinutes, assignedRunway);
            solvedFlights.Add(solved);

            runwayAvailableAt = UpdateRunway(assignedTime, separation);
        }

        return BuildResult(solvedFlights, orderedFlights.Count);
    }

    private static DateTime ComputeAssignedTime(Flight flight, DateTime? runwayAvailableAt)
    {
        if (runwayAvailableAt == null)
            return flight.ScheduledTime;

        return runwayAvailableAt > flight.ScheduledTime
            ? runwayAvailableAt.Value
            : flight.ScheduledTime;
    }

    private static int ComputeDelay(Flight flight, DateTime assignedTime)
    {
        return (int)Math.Max(0, (assignedTime - flight.ScheduledTime).TotalMinutes);
    }

    private static DateTime UpdateRunway(DateTime assignedTime, TimeSpan separation)
    {
        return assignedTime.Add(separation);
    }

    private static string? ChooseRunway(ScenarioSnapshot snapshot, Flight flight, DateTime assignedTime)
    {
        return "RWY-UNASSIGNED";
    }

    private static SolvedFlight CreateSolvedFlight(
        Flight flight,
        DateTime assignedTime,
        int delayMinutes,
        string? assignedRunway)
    {
        return new SolvedFlight
        {
            FlightId = flight.Id,
            ScenarioConfigId = flight.ScenarioConfigId,
            AircraftId = flight.AircraftId,

            Callsign = flight.Callsign,
            Type = flight.Type,

            ScheduledTime = flight.ScheduledTime,
            MaxDelayMinutes = flight.MaxDelayMinutes,
            MaxEarlyMinutes = flight.MaxEarlyMinutes,
            Priority = flight.Priority,

            AssignedTime = assignedTime,
            AssignedRunway = assignedRunway,
            DelayMinutes = delayMinutes,
            EarlyMinutes = 0
        };
    }

    private static List<Flight> OrderFlights(IReadOnlyList<Flight> flights)
    {
        return flights
            .OrderBy(f => f.ScheduledTime)
            .ThenByDescending(f => f.Priority)
            .ToList();
    }

    private static TimeSpan CalculateSeparation(ScenarioSnapshot snapshot)
    {
        var config = snapshot.ScenarioConfig;

        var totalSeconds =
            config.BaseSeparationSeconds *
            (config.WakePercent / 100.0) *
            (config.WeatherPercent / 100.0);

        return TimeSpan.FromSeconds(totalSeconds);
    }

    private static bool CanFitAllFlights(ScenarioSnapshot snapshot, int flightCount, TimeSpan separation)
    {
        var totalTimeNeeded = TimeSpan.FromTicks(separation.Ticks * (flightCount - 1));

        var availableTime =
            snapshot.ScenarioConfig.EndTime - snapshot.ScenarioConfig.StartTime;

        return totalTimeNeeded <= availableTime;
    }

    private static bool IsWithinScenario(ScenarioSnapshot snapshot, DateTime time)
    {
        return time >= snapshot.ScenarioConfig.StartTime &&
               time <= snapshot.ScenarioConfig.EndTime;
    }

    private static bool IsDelayTooLarge(Flight flight, int delayMinutes)
    {
        return delayMinutes > flight.MaxDelayMinutes;
    }

    private static SolvedFlight CreateCanceledFlight(Flight flight)
    {
        return new SolvedFlight
        {
            FlightId = flight.Id,
            ScenarioConfigId = flight.ScenarioConfigId,
            AircraftId = flight.AircraftId,

            Callsign = flight.Callsign,
            Type = flight.Type,

            ScheduledTime = flight.ScheduledTime,
            MaxDelayMinutes = flight.MaxDelayMinutes,
            MaxEarlyMinutes = flight.MaxEarlyMinutes,
            Priority = flight.Priority,

            AssignedTime = null,
            AssignedRunway = null,
            DelayMinutes = 0,
            EarlyMinutes = 0
        };
    }

    private static SolverResult BuildResult(List<SolvedFlight> flights, int total)
    {
        var delayed = flights.Count(f => f.AssignedTime != null && f.DelayMinutes > 0);
        var canceled = flights.Count(f => f.AssignedTime == null);
        var totalDelay = flights.Sum(f => f.DelayMinutes);

        return new SolverResult
        {
            Flights = flights,
            TotalFlights = total,
            TotalDelayMinutes = totalDelay,
            TotalCanceledFlights = canceled,
            TotalDelayedFlights = delayed
        };
    }
}