using MediatR;
using Modules.Aircrafts.Application.UseCases.GenerateRandomAircraft;
using Modules.Airports.Application.UseCases.CreateAirport;
using Modules.Airports.Application.UseCases.GetAirports;
using Modules.Airports.Application.UseCases.CreateRunway;
using Modules.Airports.Application.UseCases.GetRunwaysByAirportId;
using Modules.Scenarios.Application.UseCases.CreateScenarioConfig;
using Modules.Scenarios.Application.UseCases.GetScenarioConfigs;
using Modules.Airports.Application.UseCases.DeleteRunway;
using Modules.Airports.Application.UseCases.UpdateRunway;
using Modules.Aircrafts.Application.UseCases.GetAircrafts;
using Modules.Scenarios.Application.UseCases.CreateFlights;
using Modules.Scenarios.Application.UseCases.DeleteScenario;
using Modules.Scenarios.Application.UseCases.CreateWeatherIntervals;
using Modules.Scenarios.Application.UseCases.GetWeatherIntervals;
using Modules.Scenarios.Application.UseCases.GetFlights;
using Modules.Scenarios.Application.UseCases.GetAllDataScenarioConfig;
using Modules.Airports.Application.UseCases.DeleteAirport;

namespace Api;

public static class Endpoints
{
    public static void MapAll(WebApplication app)
    {
        app.MapPost("/aircrafts/generate/{ScenarioId:guid}",
    async (Guid scenarioId, GenerateRandomAircraftCommand cmd, IMediator mediator, CancellationToken ct) =>
    {
        var res = await mediator.Send(cmd, ct);
        return Results.Ok(res);
    })
    .WithName("GenerateRandomAircraftsByAirport");

        app.MapGet("/aircrafts/{ScenarioId:guid}",
        async (Guid scenarioId, IMediator mediator) =>
        {
            var res = await mediator.Send(new GetAircraftsQuery(scenarioId));
            return Results.Ok(res);
        })
        .WithName("GetAircraftsByScenarioId");

        app.MapPost("/airport",
            async (CreateAirportCommand cmd, IMediator mediator) =>
            {
                var res = await mediator.Send(cmd);
                return Results.Ok(res);
            })
            .WithName("CreateAirport");

        app.MapGet("/airports",
        async (IMediator mediator) =>
        {
            var res = await mediator.Send(new GetAirportsQuery());
            return Results.Ok(res);
        }).WithName("GetAirports");

        app.MapPost("/airports/{airportId:guid}/runways",
      async (Guid airportId, CreateRunwayCommand cmd, IMediator mediator) =>
      {
          var res = await mediator.Send(cmd with { AirportId = airportId });
          return Results.Ok(res);
      })
      .WithName("CreateRunway");

        app.MapDelete("/runways/{runwayId:guid}",
    async (Guid runwayId, IMediator mediator) =>
    {
        var res = await mediator.Send(new DeleteRunwayCommand(runwayId));

        if (res)
            return Results.NoContent();

        return Results.NotFound();
    })
    .WithName("DeleteRunway");


        app.MapPut("/runways/{runwayId:guid}",
        async (Guid runwayId, UpdateRunwayCommand cmd, IMediator mediator) =>
        {
            var ok = await mediator.Send(cmd with { RunwayId = runwayId });

            if (ok) return Results.NoContent();
            return Results.NotFound();
        })
        .WithName("UpdateRunway");

        app.MapGet("/airports/{airportId:guid}/runways",
        async (Guid airportId, IMediator mediator) =>
        {
            var res = await mediator.Send(new GetRunwaysByAirportIdQuery(airportId));
            return Results.Ok(res);
        })
        .WithName("GetRunwaysByAirportId");

        app.MapPost("/scenarios/configs",
        async (CreateScenarioConfigCommand cmd, IMediator mediator) =>
        {
            var res = await mediator.Send(cmd);
            return Results.Ok(res);
        })
        .WithName("CreateScenarioConfig");

        app.MapGet("/scenarios/configs", async (IMediator mediator) =>
        {
            var res = await mediator.Send(new ScenarioConfigQuery());
            return Results.Ok(res);
        })
            .WithName("GetScenarioConfigs");


        app.MapPost("/flights/generate/{ScenarioConfigId:guid}",
            async (Guid scenarioConfigId, IMediator mediator, CancellationToken ct) =>
            {
                var res = await mediator.Send(new CreateFlightsCommand(scenarioConfigId), ct);
                return Results.Ok(res);
            })
            .WithName("CreateFlightsByScenarioConfig");

        app.MapDelete("/scenarios/configs/{ScenarioConfigId:guid}",
          async (Guid scenarioConfigId, IMediator mediator) =>
         {
             var res = await mediator.Send(new DeleteScenarioCommand(scenarioConfigId));
             if (res)
                 return Results.NoContent();
             return Results.NotFound();
         })
         .WithName("DeleteScenarioConfig");

        app.MapPost("/weatherintervals/generate/{ScenarioConfigId:guid}",
            async (Guid scenarioConfigId, IMediator mediator, CancellationToken ct) =>
            {
                var res = await mediator.Send(new CreateWeatherIntervalsCommand(scenarioConfigId), ct);
                return Results.Ok(res);
            })
            .WithName("CreateWeatherIntervalsByScenarioConfig");

        app.MapGet("/weatherintervals/{ScenarioConfigId:guid}",
        async (Guid scenarioConfigId, IMediator mediator, CancellationToken ct) =>
        {
            var res = await mediator.Send(new WeatherIntervalsQuery(scenarioConfigId), ct);
            return Results.Ok(res);
        })
        .WithName("GetWeatherIntervalsByScenarioConfig");

        app.MapGet("/flights/{scenarioConfigId:guid}",
       async (Guid scenarioConfigId, IMediator mediator, CancellationToken ct) =>
        {
            var res = await mediator.Send(new FlightQuery(scenarioConfigId), ct);
            return Results.Ok(res);
        })
            .WithName("GetFlightsByScenarioConfig");


        app.MapGet("/scenarios/configs/{scenarioConfigId:guid}",
        async (Guid scenarioConfigId, IMediator mediator,CancellationToken ct) =>
        {
            var res = await mediator.Send(new GetAllDataScenarioConfigQuery(scenarioConfigId),ct);
            return Results.Ok(res);
        })
        .WithName("GetAllDataScenarioConfig");

        app.MapDelete("/airports/{airportId:guid}",
        async (Guid airportId, IMediator mediator, CancellationToken ct) =>
        {            var res = await mediator.Send(new DeleteAirportCommand(airportId), ct);
            if (res)
                return Results.NoContent();
            return Results.NotFound();
         })
        .WithName("DeleteAirport");
    }
}
