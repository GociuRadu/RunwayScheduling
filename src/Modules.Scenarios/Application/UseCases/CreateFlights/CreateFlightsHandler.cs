using MediatR;
using Modules.Aircrafts.Application.UseCases.GenerateRandomAircraft;
using Modules.Scenarios.Application.Services;
using Modules.Scenarios.Domain;
using Modules.Scenarios.Domain.Exceptions;

namespace Modules.Scenarios.Application.UseCases.CreateFlights;

public sealed class CreateFlightsHandler : IRequestHandler<CreateFlightsCommand, List<Flight>>
{
    private readonly IMediator _mediator;
    private readonly IScenarioConfigStore _configStore;
    private readonly IFlightStore _flightStore;
    private readonly FlightScheduler _scheduler;

    public CreateFlightsHandler(
        IMediator mediator,
        IScenarioConfigStore configStore,
        IFlightStore flightStore,
        FlightScheduler scheduler)
    {
        _mediator = mediator;
        _configStore = configStore;
        _flightStore = flightStore;
        _scheduler = scheduler;
    }

    public async Task<List<Flight>> Handle(CreateFlightsCommand request, CancellationToken ct)
    {
        var cfg = await _configStore.GetById(request.ScenarioConfigId, ct)
            ?? throw new ScenarioConfigNotFoundException(request.ScenarioConfigId);

        ValidateConfig(cfg);

        var aircrafts = await _mediator.Send(
            new GenerateRandomAircraftCommand(cfg.AircraftCount, cfg.AircraftDifficulty, cfg.Id), ct);

        if (aircrafts.Count < cfg.AircraftCount)
            throw new InvalidScenarioConfigException("Generated aircraft count must be >= ScenarioConfig.AircraftCount");

        var flights = _scheduler.Schedule(aircrafts, cfg);

        await _flightStore.AddRange(flights, ct);
        await _flightStore.SaveChanges(ct);

        return flights;
    }

    private static void ValidateConfig(ScenarioConfig cfg)
    {
        if (cfg.OnGroundAircraftCount + cfg.InboundAircraftCount != cfg.AircraftCount)
            throw new InvalidScenarioConfigException("OnGroundAircraftCount + InboundAircraftCount must equal AircraftCount");

        if (cfg.RemainingOnGroundAircraftCount < 0 || cfg.RemainingOnGroundAircraftCount > cfg.AircraftCount)
            throw new InvalidScenarioConfigException("RemainingOnGroundAircraftCount must be in [0..AircraftCount]");

        if ((cfg.EndTime - cfg.StartTime).TotalMinutes < 10)
            throw new InvalidScenarioConfigException("Scenario interval must be at least 10 minutes.");
    }
}
