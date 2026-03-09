using MediatR;

namespace Modules.Airports.Application.UseCases.GetAirports;

public sealed record GetAirportsQuery() : IRequest<List<AirportDto>>;
