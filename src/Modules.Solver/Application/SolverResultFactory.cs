using Modules.Solver.Domain;

namespace Modules.Solver.Application;

internal static class SolverResultFactory
{
    public static SolverResult Create(string algorithmName, IReadOnlyList<SolvedFlight> flights, int totalFlights, DateTime scenarioStart, DateTime scenarioEnd, double solveTimeMs)
    {
        var scheduledFlights = flights.Count(flight => flight.Status != FlightStatus.Canceled);
        var totalDelayMinutes = flights.Sum(flight => flight.DelayMinutes);
        var scenarioHours = (scenarioEnd - scenarioStart).TotalHours;

        return new SolverResult
        {
            AlgorithmName = algorithmName,
            Flights = flights,
            TotalFlights = totalFlights,
            TotalScheduledFlights = scheduledFlights,
            TotalOnTimeFlights = flights.Count(flight => flight.Status == FlightStatus.Scheduled),
            TotalEarlyFlights = flights.Count(flight => flight.Status == FlightStatus.Early),
            TotalDelayedFlights = flights.Count(flight => flight.Status == FlightStatus.Delayed),
            TotalCanceledFlights = flights.Count(flight => flight.Status == FlightStatus.Canceled),
            TotalRescheduledFlights = flights.Count(flight => flight.Status == FlightStatus.Rescheduled),
            TotalDelayMinutes = totalDelayMinutes,
            AverageDelayMinutes = scheduledFlights > 0 ? (double)totalDelayMinutes / scheduledFlights : 0.0,
            MaxDelayMinutes = flights.Count > 0 ? flights.Max(flight => flight.DelayMinutes) : 0,
            SolveTimeMs = solveTimeMs,
            ThroughputFlightsPerHour = scenarioHours > 0 ? scheduledFlights / scenarioHours : 0.0
        };
    }
}
