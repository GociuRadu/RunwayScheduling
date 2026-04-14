using Microsoft.EntityFrameworkCore;
using Modules.Scenarios.Application;
using Modules.Scenarios.Domain;

namespace Api.DataBase;

public sealed class EfWeatherIntervalStore : IWeatherIntervalStore
{
    private readonly AppDbContext _db;

    public EfWeatherIntervalStore(AppDbContext db) => _db = db;

    public async Task AddRange(List<WeatherInterval> weatherIntervals, CancellationToken ct)
    {
        if (weatherIntervals.Count == 0)
            return;

        _db.WeatherIntervals.AddRange(weatherIntervals);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<WeatherInterval>> GetByScenarioConfigId(Guid scenarioConfigId, CancellationToken ct)
    {
        return _db.WeatherIntervals
            .AsNoTracking()
            .Where(w => w.ScenarioConfigId == scenarioConfigId)
            .ToListAsync(ct);
    }
}
