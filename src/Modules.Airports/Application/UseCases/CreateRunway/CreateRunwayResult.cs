using System;
using Modules.Airports.Domain;

namespace Modules.Airports.Application.UseCases.CreateRunway;

public sealed record CreateRunwayResult(
    Guid Id, Guid AirportId, string Name, bool IsActive, RunwayType RunwayType
);
