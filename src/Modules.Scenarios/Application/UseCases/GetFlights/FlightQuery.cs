using MediatR;
using Modules.Scenarios.Domain;

namespace Modules.Scenarios.Application.UseCases.GetFlights;

public sealed record FlightQuery(Guid ScenarioConfigId) : IRequest<IReadOnlyList<FlightDto>>;