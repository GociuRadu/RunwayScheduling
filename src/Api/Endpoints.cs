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
using Modules.Login.Application.UseCases.Login;
using Modules.Scenarios.Application.UseCases.CreateRandomEvent;
using Modules.Scenarios.Application.UseCases.DeleteRandomEvent;
using Modules.Scenarios.Application.UseCases.GetRandomEventsByScenarioConfigId;
using Modules.Scenarios.Application.UseCases.UpdateRandomEvent;
using Modules.Solver.Application.GreedySolver;
namespace Api;

public static class Endpoints
{
    public static void MapAll(WebApplication app)
    {
        var secured = app.MapGroup("");
        secured.RequireAuthorization();

        secured.MapPost("/aircrafts/generate/{ScenarioId:guid}",
            async (Guid scenarioId, GenerateRandomAircraftCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                var res = await mediator.Send(cmd, ct);
                return Results.Ok(res);
            })
            .WithName("GenerateRandomAircraftsByAirport");

        secured.MapGet("/aircrafts/{ScenarioId:guid}",
            async (Guid scenarioId, IMediator mediator) =>
            {
                var res = await mediator.Send(new GetAircraftsQuery(scenarioId));
                return Results.Ok(res);
            })
            .WithName("GetAircraftsByScenarioId");

        secured.MapPost("/airport",
            async (CreateAirportCommand cmd, IMediator mediator) =>
            {
                var res = await mediator.Send(cmd);
                return Results.Ok(res);
            })
            .WithName("CreateAirport");

        secured.MapGet("/airports",
            async (IMediator mediator) =>
            {
                var res = await mediator.Send(new GetAirportsQuery());
                return Results.Ok(res);
            })
            .WithName("GetAirports");

        secured.MapPost("/airports/{airportId:guid}/runways",
            async (Guid airportId, CreateRunwayCommand cmd, IMediator mediator) =>
            {
                var res = await mediator.Send(cmd with { AirportId = airportId });
                return Results.Ok(res);
            })
            .WithName("CreateRunway");

        secured.MapDelete("/runways/{runwayId:guid}",
            async (Guid runwayId, IMediator mediator) =>
            {
                var res = await mediator.Send(new DeleteRunwayCommand(runwayId));

                if (res)
                    return Results.NoContent();

                return Results.NotFound();
            })
            .WithName("DeleteRunway");

        secured.MapPut("/runways/{runwayId:guid}",
            async (Guid runwayId, UpdateRunwayCommand cmd, IMediator mediator) =>
            {
                var ok = await mediator.Send(cmd with { RunwayId = runwayId });

                if (ok) return Results.NoContent();
                return Results.NotFound();
            })
            .WithName("UpdateRunway");

        secured.MapGet("/airports/{airportId:guid}/runways",
            async (Guid airportId, IMediator mediator) =>
            {
                var res = await mediator.Send(new GetRunwaysByAirportIdQuery(airportId));
                return Results.Ok(res);
            })
            .WithName("GetRunwaysByAirportId");

        secured.MapPost("/scenarios/configs",
            async (CreateScenarioConfigCommand cmd, IMediator mediator) =>
            {
                var res = await mediator.Send(cmd);
                return Results.Ok(res);
            })
            .WithName("CreateScenarioConfig");

        secured.MapGet("/scenarios/configs",
            async (IMediator mediator) =>
            {
                var res = await mediator.Send(new ScenarioConfigQuery());
                return Results.Ok(res);
            })
            .WithName("GetScenarioConfigs");

        secured.MapPost("/flights/generate/{ScenarioConfigId:guid}",
            async (Guid scenarioConfigId, IMediator mediator, CancellationToken ct) =>
            {
                var res = await mediator.Send(new CreateFlightsCommand(scenarioConfigId), ct);
                return Results.Ok(res);
            })
            .WithName("CreateFlightsByScenarioConfig");

        secured.MapDelete("/scenarios/configs/{ScenarioConfigId:guid}",
            async (Guid scenarioConfigId, IMediator mediator) =>
            {
                var res = await mediator.Send(new DeleteScenarioCommand(scenarioConfigId));
                if (res)
                    return Results.NoContent();

                return Results.NotFound();
            })
            .WithName("DeleteScenarioConfig");

        secured.MapPost("/weatherintervals/generate/{ScenarioConfigId:guid}",
            async (Guid scenarioConfigId, IMediator mediator, CancellationToken ct) =>
            {
                var res = await mediator.Send(new CreateWeatherIntervalsCommand(scenarioConfigId), ct);
                return Results.Ok(res);
            })
            .WithName("CreateWeatherIntervalsByScenarioConfig");

        secured.MapGet("/weatherintervals/{ScenarioConfigId:guid}",
            async (Guid scenarioConfigId, IMediator mediator, CancellationToken ct) =>
            {
                var res = await mediator.Send(new WeatherIntervalsQuery(scenarioConfigId), ct);
                return Results.Ok(res);
            })
            .WithName("GetWeatherIntervalsByScenarioConfig");

        secured.MapGet("/flights/{scenarioConfigId:guid}",
            async (Guid scenarioConfigId, IMediator mediator, CancellationToken ct) =>
            {
                var res = await mediator.Send(new FlightQuery(scenarioConfigId), ct);
                return Results.Ok(res);
            })
            .WithName("GetFlightsByScenarioConfig");

        secured.MapGet("/scenarios/configs/{scenarioConfigId:guid}",
            async (Guid scenarioConfigId, IMediator mediator, CancellationToken ct) =>
            {
                var res = await mediator.Send(new GetAllDataScenarioConfigQuery(scenarioConfigId), ct);
                return Results.Ok(res);
            })
            .WithName("GetAllDataScenarioConfig");

        secured.MapDelete("/airports/{airportId:guid}",
            async (Guid airportId, IMediator mediator, CancellationToken ct) =>
            {
                var res = await mediator.Send(new DeleteAirportCommand(airportId), ct);
                if (res)
                    return Results.NoContent();

                return Results.NotFound();
            })
            .WithName("DeleteAirport");

        app.MapPost("/login",
     async (LoginCommand cmd, IMediator mediator, CancellationToken ct) =>
     {
         try
         {
             var res = await mediator.Send(cmd, ct);
             return Results.Ok(res);
         }
         catch (UnauthorizedAccessException)
         {
             return Results.Unauthorized();
         }
     })
     .AllowAnonymous()
     .RequireRateLimiting("login")
     .WithName("Login");

        secured.MapPost("/scenarios/{scenarioConfigId:guid}/random-events",
       async (Guid scenarioConfigId, CreateRandomEventCommand cmd, IMediator mediator, CancellationToken ct) =>
       {
           var res = await mediator.Send(cmd with { ScenarioConfigId = scenarioConfigId }, ct);
           return Results.Ok(res);
       })
       .WithName("CreateRandomEvent");


        secured.MapDelete("/random-events/{randomEventId:guid}",
         async (Guid randomEventId, IMediator mediator, CancellationToken ct) =>
         {
             var res = await mediator.Send(new DeleteRandomEventCommand(randomEventId), ct);

             if (res)
                 return Results.NoContent();

             return Results.NotFound();
         })
         .WithName("DeleteRandomEvent");

        secured.MapGet("/random-events/{ScenarioConfigId:guid}",
        async (Guid ScenarioConfigId, IMediator mediator, CancellationToken ct) =>
        {
            var res = await mediator.Send(new GetRandomEventsByScenarioConfigIdQuery(ScenarioConfigId), ct);

            return Results.Ok(res);
        })
        .WithName("GetRandomEventByScenarioId");

        secured.MapPut("/random-events/{randomEventId:guid}",
    async (Guid randomEventId, UpdateRandomEventCommand request, IMediator mediator, CancellationToken ct) =>
    {
        var res = await mediator.Send(
            new UpdateRandomEventCommand(
                randomEventId,
                request.ScenarioConfigId,
                request.Name,
                request.Description,
                request.StartTime,
                request.EndTime,
                request.ImpactPercent
            ),
            ct
        );

        if (res is null)
            return Results.NotFound();

        return Results.Ok(res);
    })
    .WithName("UpdateRandomEvent");

        secured.MapGet("/greedy/{scenarioConfigId:guid}",
         async (Guid scenarioConfigId, IMediator mediator, CancellationToken ct) =>
         {
             var res = await mediator.Send(new GreedySolverQuery(scenarioConfigId), ct);

             return Results.Ok(res);
         })
         .WithName("SolveScenarioGreedy");

    }
}
