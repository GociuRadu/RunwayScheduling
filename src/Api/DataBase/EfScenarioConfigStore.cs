using Microsoft.EntityFrameworkCore;
using Modules.Scenarios.Application;
using Modules.Scenarios.Domain;
using System.Linq;
namespace Api.DataBase;

public sealed class EfScenarioConfigStore : IScenarioConfigStore
{
    private readonly AppDbContext _db;

    public EfScenarioConfigStore(AppDbContext db) => _db = db;

    public async Task<ScenarioConfig> Add(ScenarioConfig config, CancellationToken ct)
    {
        await _db.ScenarioConfigs.AddAsync(config, ct);
        await _db.SaveChangesAsync(ct);
        return config;
    }

    public Task<List<ScenarioConfig>> GetAll(CancellationToken ct) =>
        _db.ScenarioConfigs.AsNoTracking().ToListAsync(ct);

    public Task<ScenarioConfig?> GetById(Guid id, CancellationToken ct) =>
       _db.ScenarioConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<bool> Delete(Guid id, CancellationToken ct)
    {
        var config = await _db.ScenarioConfigs.FindAsync(new object[] { id }, ct);

        if (config is null)
            return false;

        _db.ScenarioConfigs.Remove(config);
        await _db.SaveChangesAsync(ct);
        return true;
    }

}
