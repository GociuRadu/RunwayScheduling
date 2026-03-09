using MediatR;
using Modules.Airports.Application;

namespace Modules.Airports.Application.UseCases.GetAirports;

public sealed class GetAirportsHandler : IRequestHandler<GetAirportsQuery, List<AirportDto>>
{
    private readonly IAirportStore _store;

    public GetAirportsHandler(IAirportStore store)
    {
        _store = store;
    }

    public Task<List<AirportDto>> Handle(GetAirportsQuery request, CancellationToken ct)
    {
        var airports = _store.GetAll();

        var res = airports
            .Select(a => new AirportDto(a.Id, a.Name,a.StandCapacity, a.Latitude, a.Longitude))
            .ToList();

        return Task.FromResult(res);
    }
}
