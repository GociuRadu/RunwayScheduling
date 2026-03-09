using System;
using Modules.Airports.Domain;

namespace Modules.Airports.Application.UseCases.GetRunwaysByAirportId;

public sealed record RunwayDto
(
    Guid id, Guid AirportId, string Name, bool IsActive, RunwayType RunwayType
);
