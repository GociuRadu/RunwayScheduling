using Microsoft.EntityFrameworkCore;
using Modules.Scenarios.Application;
using Modules.Scenarios.Domain;

namespace Api.DataBase;

public sealed class EfFlightStore : IFlightStore
{
    private readonly AppDbContext _db;

    public EfFlightStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddRange(List<Flight> flights, CancellationToken ct)
    {
        if (flights.Count == 0)
            return;

        await _db.Flights.AddRangeAsync(flights, ct);
    }

    public Task SaveChanges(CancellationToken ct)
        => _db.SaveChangesAsync(ct);

    public Task<List<Flight>> GetByScenarioConfigId(Guid scenarioConfigId, CancellationToken ct)
    {
        return _db.Flights
            .AsNoTracking()
            .Where(f => f.ScenarioConfigId == scenarioConfigId)
            .ToListAsync(ct);
    }
}
