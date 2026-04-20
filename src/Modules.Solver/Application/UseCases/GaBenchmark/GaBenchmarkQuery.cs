using MediatR;

namespace Modules.Solver.Application.UseCases.GaBenchmark;

public sealed record GaBenchmarkQuery(
    IReadOnlyList<Guid> ScenarioConfigIds,
    IReadOnlyList<GaConfigParams> Configs)
    : IRequest<GaBenchmarkResult>;
