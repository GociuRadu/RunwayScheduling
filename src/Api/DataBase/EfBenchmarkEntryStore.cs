using Microsoft.EntityFrameworkCore;
using Modules.Solver.Application;
using Modules.Solver.Domain;

namespace Api.DataBase;

public sealed class EfBenchmarkEntryStore(AppDbContext db) : IBenchmarkEntryStore
{
    public async Task AddRangeAsync(IEnumerable<BenchmarkEntry> entries, CancellationToken ct)
    {
        db.BenchmarkEntries.AddRange(entries);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<BenchmarkEntry>> GetAllAsync(CancellationToken ct) =>
        await db.BenchmarkEntries
            .OrderBy(e => e.ScenarioConfigId)
            .ThenBy(e => e.Fitness)
            .ToListAsync(ct);
}
