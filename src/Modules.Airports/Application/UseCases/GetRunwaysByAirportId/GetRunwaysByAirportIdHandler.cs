using MediatR;
using Modules.Airports.Application;
using System.Linq;

namespace Modules.Airports.Application.UseCases.GetRunwaysByAirportId;

public sealed class GetRunwaysByAirportIdHandler
    : IRequestHandler<GetRunwaysByAirportIdQuery, List<RunwayDto>>
{
    private readonly IRunwayStore _store;

    public GetRunwaysByAirportIdHandler(IRunwayStore store)
    {
        _store = store;
    }

    public Task<List<RunwayDto>> Handle(GetRunwaysByAirportIdQuery request, CancellationToken ct)
    {
        var runways = _store.GetByAirportId(request.AirportId);

        var res = runways
            .Select(r => new RunwayDto(r.Id, r.AirportId, r.Name, r.IsActive, r.RunwayType))
            .ToList();

        return Task.FromResult(res);
    }
}
