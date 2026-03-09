using MediatR;

namespace Modules.Airports.Application.UseCases.DeleteRunway;

public sealed record DeleteRunwayCommand(Guid RunwayId) : IRequest<bool>;