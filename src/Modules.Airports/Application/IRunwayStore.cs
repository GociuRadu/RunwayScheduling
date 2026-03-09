using Modules.Airports.Domain;
namespace Modules.Airports.Application;

public interface IRunwayStore
{
    Runway Add(Runway runway);
    IEnumerable<Runway> GetByAirportId(Guid airportId);
    Task<bool> Delete(Guid runwayId, CancellationToken ct);
    Task<bool> Update(Guid runwayId, string name, bool isActive, RunwayType runwayType, CancellationToken ct);
}
