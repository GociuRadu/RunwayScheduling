using System;
using System.ComponentModel.DataAnnotations;
using MediatR;
using Modules.Airports.Domain;

namespace Modules.Airports.Application.UseCases.CreateRunway;

public sealed record CreateRunwayCommand(
    [property: Required]
    Guid AirportId,
    [property: Required]
    [property: StringLength(32, MinimumLength = 2)]
    string Name,
    bool IsActive,
    RunwayType RunwayType)
: IRequest<CreateRunwayResult>;
