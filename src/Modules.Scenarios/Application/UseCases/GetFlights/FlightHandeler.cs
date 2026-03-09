using MediatR;
namespace Modules.Scenarios.Application.UseCases.GetFlights;

public sealed class FlightHandler : IRequestHandler<FlightQuery, IReadOnlyList<FlightDto>>
{
    private readonly IFlightStore _flightStore;

    public FlightHandler(IFlightStore flightStore) => _flightStore = flightStore;

    public async Task<IReadOnlyList<FlightDto>> Handle(FlightQuery request, CancellationToken ct)
    {
        var flights = await _flightStore.GetByScenarioConfigId(request.ScenarioConfigId, ct);

        return flights.Select(f => new FlightDto(
            f.Id,
            f.ScenarioConfigId,
            f.AircraftId,
            f.Callsign,
            f.Type,
            f.ScheduledTime,
            f.MaxDelayMinutes,
            f.MaxEarlyMinutes,
            f.Priority
        )).ToList();
    }
}