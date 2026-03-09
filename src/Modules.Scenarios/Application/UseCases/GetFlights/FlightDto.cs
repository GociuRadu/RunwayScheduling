using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application.UseCases.GetFlights;

public sealed record FlightDto(
    Guid Id,
    Guid ScenarioConfigId,
    Guid AircraftId,
    string Callsign,
    FlightType Type,
    DateTime ScheduledTime,
    int MaxDelayMinutes,
    int MaxEarlyMinutes,
    int Priority

);