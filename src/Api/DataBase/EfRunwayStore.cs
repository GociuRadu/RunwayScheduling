using System;
using System.Collections.Generic;
using System.Linq;
using Modules.Airports.Application;
using Modules.Airports.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.DataBase;

public sealed class EfRunwayStore : IRunwayStore
{
    private readonly AppDbContext _db;

    public EfRunwayStore(AppDbContext db)
    {
        _db = db;
    }

    public Runway Add(Runway runway)
    {
        _db.Runways.Add(runway);
        _db.SaveChanges();
        return runway;
    }

    public IEnumerable<Runway> GetByAirportId(Guid airportId)
    {
        return _db.Runways
            .Where(r => r.AirportId == airportId)
            .ToList();
    }

    public async Task<bool> Delete(Guid runwayId, CancellationToken ct)
    {
        var runway = await _db.Runways.FindAsync(new object[] { runwayId }, ct);

        if (runway is null)
            return false;

        _db.Runways.Remove(runway);
        await _db.SaveChangesAsync(ct);

        return true;
    }
    public async Task<bool> Update(Guid runwayId, string name, bool isActive, RunwayType runwayType, CancellationToken ct)
    {
        var runway = await _db.Runways.FirstOrDefaultAsync(r => r.Id == runwayId, ct);
        if (runway is null) return false;

        runway.Name = name;
        runway.IsActive = isActive;
        runway.RunwayType = runwayType;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}
