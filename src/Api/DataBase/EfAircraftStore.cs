using Microsoft.EntityFrameworkCore;
using Modules.Aircrafts.Application;
using Modules.Aircrafts.Domain;

namespace Api.DataBase;

public sealed class EfAircraftStore : IAircraftStore
{
    private readonly AppDbContext _db;

    public EfAircraftStore(AppDbContext db) => _db = db;

    public async Task AddRange(List<Aircraft> aircrafts, CancellationToken ct)
    {
        if (aircrafts is null || aircrafts.Count == 0)
            return;

        await _db.Aircrafts.AddRangeAsync(aircrafts, ct);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<Aircraft>> GetByScenarioId(Guid scenarioConfigId, CancellationToken ct)
    {
        return _db.Aircrafts
            .AsNoTracking()
            .Where(a => a.ScenarioConfigId == scenarioConfigId)
            .ToListAsync(ct);
    }
}