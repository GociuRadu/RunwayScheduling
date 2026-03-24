using MediatR;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GreedySolver;

public sealed record GreedySolverQuery(Guid ScenarioConfigId) : IRequest<SolverResult>;