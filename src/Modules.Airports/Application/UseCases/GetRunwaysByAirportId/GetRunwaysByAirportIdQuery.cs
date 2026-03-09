using MediatR;
using Modules.Airports.Domain;
namespace Modules.Airports.Application.UseCases.GetRunwaysByAirportId;

public sealed record GetRunwaysByAirportIdQuery(Guid AirportId) : IRequest<List<RunwayDto>> { }