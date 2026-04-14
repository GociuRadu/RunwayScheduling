using MediatR;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.UseCases.Compare;

public sealed record CompareQuery(Guid ScenarioConfigId) : IRequest<CompareResult>;

public sealed record CompareResult(SolverResult Greedy, SolverResult Genetic);
