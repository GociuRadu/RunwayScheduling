using MediatR;
using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application.UseCases.CreateFlights;

public sealed record CreateFlightsCommand(Guid ScenarioConfigId) : IRequest<List<Flight>>;