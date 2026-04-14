using MediatR;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.UseCases.SolveGenetic;

public sealed record SolveGeneticQuery(Guid ScenarioConfigId) : IRequest<SolverResult>;
