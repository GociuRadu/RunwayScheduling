using System.ComponentModel.DataAnnotations;
using MediatR;
using Modules.Airports.Domain;

namespace Modules.Airports.Application.UseCases.UpdateRunway;

public sealed record UpdateRunwayCommand(
    [property: Required]
    Guid RunwayId,
    [property: Required]
    [property: StringLength(32, MinimumLength = 2)]
    string Name,
    bool IsActive,
    RunwayType RunwayType
) : IRequest<bool>;
