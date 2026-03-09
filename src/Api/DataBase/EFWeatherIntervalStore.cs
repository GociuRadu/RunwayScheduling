using Modules.Scenarios.Application;
using Modules.Scenarios.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq;
namespace Api.DataBase;

public sealed class EFWeatherIntervalStore : IWeatherIntervalStore
{
    private readonly AppDbContext _db;

    public EFWeatherIntervalStore(AppDbContext db) => _db = db;

    public async Task AddRange(List<WeatherInterval> weatherIntervals, CancellationToken ct)
    {
        _db.WeatherIntervals.AddRange(weatherIntervals);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<WeatherInterval>> GetByScenarioConfigId(Guid scenarioConfigId, CancellationToken ct)
    {
        return await _db.WeatherIntervals.Where(w => w.ScenarioConfigId == scenarioConfigId).ToListAsync(ct);
    }
}