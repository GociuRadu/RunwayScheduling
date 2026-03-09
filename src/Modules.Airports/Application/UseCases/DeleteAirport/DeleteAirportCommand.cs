using MediatR;

namespace Modules.Airports.Application.UseCases.DeleteAirport;

public sealed record DeleteAirportCommand(Guid AirportId) : IRequest<bool>;