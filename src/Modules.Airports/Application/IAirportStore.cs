using System.Collections.Generic;
using Modules.Airports.Domain;

namespace Modules.Airports.Application;

public interface IAirportStore
{
    Airport Add(Airport airport);
    List<Airport> GetAll();
    Task<bool> Delete(Guid airportId, CancellationToken ct);
}
