using Microsoft.EntityFrameworkCore;
using Modules.Scenarios.Application;
using Modules.Scenarios.Application.UseCases.GetRandomEventsByScenarioConfigId;
using Modules.Scenarios.Domain;

namespace Api.DataBase;

public sealed class EfRandomEventStore : IRandomEventStore
{
    private readonly AppDbContext _db;

    public EfRandomEventStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RandomEvent> Add(RandomEvent randomEvent, CancellationToken ct)
    {
        await _db.RandomEvents.AddAsync(randomEvent, ct);
        await _db.SaveChangesAsync(ct);
        return randomEvent;
    }

    public async Task<bool> Delete(Guid eventId, CancellationToken ct)
    {
        var randomEvent = await _db.RandomEvents.FindAsync(new object[] { eventId }, ct);

        if (randomEvent is null)
            return false;

        _db.RandomEvents.Remove(randomEvent);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<RandomEventDto>> GetAllByScenarioConfigId(Guid scenarioConfigId, CancellationToken ct)
    {
        return await _db.RandomEvents
            .AsNoTracking()
            .Where(x => x.ScenarioConfigId == scenarioConfigId)
            .Select(x => new RandomEventDto(
                x.Id,
                x.ScenarioConfigId,
                x.Name,
                x.Description,
                x.StartTime,
                x.EndTime,
                x.ImpactPercent
            ))
            .ToListAsync(ct);
    }

    public async Task<RandomEvent?> Update(
        Guid id,
        Guid scenarioConfigId,
        string name,
        string description,
        DateTime startTime,
        DateTime endTime,
        int impactPercent,
        CancellationToken ct)
    {
        var randomEvent = await _db.RandomEvents.FindAsync(new object[] { id }, ct);

        if (randomEvent is null)
            return null;

        randomEvent.ScenarioConfigId = scenarioConfigId;
        randomEvent.Name = name;
        randomEvent.Description = description;
        randomEvent.StartTime = startTime;
        randomEvent.EndTime = endTime;
        randomEvent.ImpactPercent = impactPercent;

        await _db.SaveChangesAsync(ct);
        return randomEvent;
    }
}
