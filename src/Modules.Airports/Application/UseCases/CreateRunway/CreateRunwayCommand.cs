using System;
using MediatR;
using Modules.Airports.Domain;

namespace Modules.Airports.Application.UseCases.CreateRunway;

public sealed record CreateRunwayCommand(Guid AirportId, string Name, bool IsActive, RunwayType RunwayType)
: IRequest<CreateRunwayResult>;
