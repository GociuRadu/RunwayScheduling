using Modules.Airports.Domain;

namespace Modules.Airports.Application;

public interface IAirportStore
{
    Task<Airport> AddAsync(Airport airport, CancellationToken ct);
    Task<List<Airport>> GetAllAsync(CancellationToken ct);
    Task<bool> DeleteAsync(Guid airportId, CancellationToken ct);
}
