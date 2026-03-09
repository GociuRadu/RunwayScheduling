using MediatR;
using Modules.Airports.Domain;

namespace Modules.Airports.Application.UseCases.UpdateRunway;

public sealed record UpdateRunwayCommand(
    Guid RunwayId,
    string Name,
    bool IsActive,
    RunwayType RunwayType
) : IRequest<bool>;