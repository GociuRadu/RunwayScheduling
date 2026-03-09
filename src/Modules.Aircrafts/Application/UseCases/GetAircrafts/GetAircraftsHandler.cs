using MediatR;

namespace Modules.Aircrafts.Application.UseCases.GetAircrafts;

public sealed class GetAircraftsHandler : IRequestHandler<GetAircraftsQuery, List<GetAircraftsDto>>
{
    private readonly IAircraftStore _aircraftStore;

    public GetAircraftsHandler(IAircraftStore aircraftStore)
    {
        _aircraftStore = aircraftStore;
    }

    public async Task<List<GetAircraftsDto>> Handle(GetAircraftsQuery request, CancellationToken ct)
    {
        var aircrafts = await _aircraftStore.GetByScenarioId(request.ScenarioConfigId, ct);

        var result = new List<GetAircraftsDto>();

        foreach (var a in aircrafts)
        {
            result.Add(new GetAircraftsDto(
                a.Id,
                a.TailNumber,
                a.Model,
                a.MaxPassengers,
                a.WakeCategory
            ));
        }
        return result;
    }
}