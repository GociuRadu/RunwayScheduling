using MediatR;
using AircraftEntity = Modules.Aircrafts.Domain.Aircraft;

namespace Modules.Aircrafts.Application.UseCases.GenerateRandomAircraft;

public sealed record GenerateRandomAircraftCommand(int Count, int Difficulty, Guid ScenarioConfigId)
    : IRequest<List<AircraftEntity>>;
