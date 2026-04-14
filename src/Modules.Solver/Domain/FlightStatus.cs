namespace Modules.Solver.Domain;

public enum FlightStatus
{
    Pending    = 0,   // not used by engine; aligns frontend enum
    Scheduled  = 1,
    Delayed    = 2,
    Canceled   = 3,
    Early      = 4,
    Rescheduled = 5
}
