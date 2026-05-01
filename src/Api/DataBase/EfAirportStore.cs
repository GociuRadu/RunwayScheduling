using Microsoft.EntityFrameworkCore;
using Modules.Airports.Application;
using Modules.Airports.Domain;

namespace Api.DataBase;

public sealed class EfAirportStore : IAirportStore
{
    private readonly AppDbContext _db;
    public EfAirportStore(AppDbContext db) => _db = db;

    public async Task<Airport> AddAsync(Airport airport, CancellationToken ct)
    {
        _db.Airports.Add(airport);
        await _db.SaveChangesAsync(ct);
        return airport;
    }

    public async Task<List<Airport>> GetAllAsync(CancellationToken ct)
        => await _db.Airports.AsNoTracking().ToListAsync(ct);

    public async Task<bool> DeleteAsync(Guid airportId, CancellationToken ct)
    {
        var airport = await _db.Airports.FindAsync(new object[] { airportId }, ct);
        if (airport is null) return false;
        _db.Airports.Remove(airport);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
