using Modules.Scenarios.Domain;

namespace Modules.Solver.Application.PostProcessing;

internal sealed record SlotCandidate(
    Guid   FlightId,
    string RunwayName,
    int    StartSec,
    int    EndSec,
    int    DelayMinutes,
    int    EarlyMinutes,
    int    Priority,
    bool   IsOnTime,
    WeatherCondition? WeatherAtAssignment,
    bool   AffectedByEvent
);
