using MediatR;
namespace Modules.Aircrafts.Application.UseCases.GetAircrafts;

public sealed record GetAircraftsQuery(Guid ScenarioConfigId) : IRequest<List<GetAircraftsDto>>;