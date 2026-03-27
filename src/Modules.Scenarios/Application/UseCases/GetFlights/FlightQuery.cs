using MediatR;

namespace Modules.Scenarios.Application.UseCases.GetFlights;

public sealed record FlightQuery(Guid ScenarioConfigId) : IRequest<IReadOnlyList<FlightDto>>;
