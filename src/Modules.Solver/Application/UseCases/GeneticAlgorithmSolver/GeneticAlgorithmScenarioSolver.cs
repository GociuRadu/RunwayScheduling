using MediatR;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GeneticAlgorithmSolver;

public sealed record GeneticAlgorithmScenarioSolverQuery(Guid ScenarioConfigId) : IRequest<SolverResult>;

