using MediatR;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.UseCases.SolveGreedy;

public sealed record SolveGreedyQuery(Guid ScenarioConfigId) : IRequest<SolverResult>;
