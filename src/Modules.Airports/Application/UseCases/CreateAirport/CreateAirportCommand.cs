using System.ComponentModel.DataAnnotations;
using MediatR;
using AirportEntity = Modules.Airports.Domain.Airport;

namespace Modules.Airports.Application.UseCases.CreateAirport;

public sealed record CreateAirportCommand(
    [property: Required]
    [property: StringLength(128, MinimumLength = 2)]
    string Name,
    [property: Range(0, 1000)]
    int StandCapacity,
    [property: Range(-90d, 90d)]
    double Latitude,
    [property: Range(-180d, 180d)]
    double Longitude)
    : IRequest<AirportEntity>;
