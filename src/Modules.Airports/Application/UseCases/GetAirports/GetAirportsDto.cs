using System;

namespace Modules.Airports.Application.UseCases.GetAirports;

public sealed record AirportDto(
    Guid Id,
    string Name,
    int StandCapacity,
    double Latitude,
    double Longitude
);
