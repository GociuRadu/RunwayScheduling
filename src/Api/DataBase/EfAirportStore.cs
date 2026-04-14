using Modules.Airports.Application;
using Modules.Airports.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.DataBase;

public sealed class EfAirportStore : IAirportStore
{
    private readonly AppDbContext _db;

    public EfAirportStore(AppDbContext db) => _db = db;

    public Airport Add(Airport airport)
    {
        _db.Airports.Add(airport);
        _db.SaveChanges();
        return airport;
    }

    public List<Airport> GetAll()
    {
        return _db.Airports.AsNoTracking().ToList();
    }

    public async Task<bool> Delete(Guid airportId, CancellationToken ct)
    {
        var airport = await _db.Airports.FindAsync(new object[] { airportId }, ct);
        if (airport is null)
            return false;

        _db.Airports.Remove(airport);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
