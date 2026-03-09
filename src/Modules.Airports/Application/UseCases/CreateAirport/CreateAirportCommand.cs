using MediatR;
using AirportEntity = Modules.Airports.Domain.Airport;

namespace Modules.Airports.Application.UseCases.CreateAirport;

public sealed record CreateAirportCommand(string Name,int StandCapacity, double Latitude, double Longitude)
    : IRequest<AirportEntity>;
