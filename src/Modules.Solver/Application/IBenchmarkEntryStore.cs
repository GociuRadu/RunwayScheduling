using Modules.Solver.Domain;

namespace Modules.Solver.Application;

public interface IBenchmarkEntryStore
{
    Task AddRangeAsync(IEnumerable<BenchmarkEntry> entries, CancellationToken ct);
    Task<IReadOnlyList<BenchmarkEntry>> GetAllAsync(CancellationToken ct);
}
