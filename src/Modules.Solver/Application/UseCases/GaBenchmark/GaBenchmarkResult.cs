namespace Modules.Solver.Application.UseCases.GaBenchmark;

// Entries are interleaved by rank across scenarios:
// best of scenario[0], best of scenario[1], 2nd best of scenario[0], 2nd best of scenario[1], ...
public sealed record GaBenchmarkResult(IReadOnlyList<GaBenchmarkEntry> Entries);
