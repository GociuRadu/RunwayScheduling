using MediatR;
using Microsoft.EntityFrameworkCore;
using Api.DataBase;
using Modules.Aircrafts.Application.UseCases.GenerateRandomAircraft;
using Modules.Airports.Application;
using Modules.Airports.Application.UseCases.CreateAirport;
using Modules.Airports.Application.UseCases.GetAirports;
using Modules.Airports.Application.UseCases.GetRunwaysByAirportId;
using Modules.Scenarios.Application;
using Modules.Scenarios.Application.UseCases.CreateScenarioConfig;
using Modules.Scenarios.Application.UseCases.GetScenarioConfigs;
using Modules.Airports.Application.UseCases.DeleteRunway;
using Modules.Airports.Application.UseCases.UpdateRunway;
using Modules.Aircrafts.Application;
using Modules.Aircrafts.Application.UseCases.GetAircrafts;
using Modules.Scenarios.Application.UseCases.CreateFlights;
using Modules.Scenarios.Application.UseCases.DeleteScenario;
using Modules.Scenarios.Application.UseCases.CreateWeatherIntervals;
using Modules.Scenarios.Application.UseCases.GetWeatherIntervals;
using Modules.Scenarios.Application.UseCases.GetFlights;
using Modules.Scenarios.Application.UseCases.GetAllDataScenarioConfig;
using Modules.Airports.Application.UseCases.DeleteAirport;


var builder = WebApplication.CreateBuilder(args);

// EF Core DbContext (one per HTTP request via AddDbContext default scoped lifetime)
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Stores (DI bindings): when a handler asks for the interface, DI provides the EF implementation.
// AddScoped = one instance per HTTP request (fits DbContext + repository/store pattern).
builder.Services.AddScoped<IAirportStore, EfAirportStore>();
builder.Services.AddScoped<IRunwayStore, EfRunwayStore>();
builder.Services.AddScoped<IScenarioConfigStore, EfScenarioConfigStore>();
builder.Services.AddScoped<IAircraftStore, EfAircraftStore>();
builder.Services.AddScoped<IFlightStore, EfFlightStore>();
builder.Services.AddScoped<IWeatherIntervalStore, EFWeatherIntervalStore>();

// MediatR: registers handlers so mediator.Send(...) can find the right IRequestHandler<,> at runtime.
// RegisterServicesFromAssembly scans the given assembly for IRequestHandler implementations.
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GenerateRandomAircraftHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GetAircraftsHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateAirportHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GetAirportsHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GetRunwaysByAirportIdHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateScenarioConfigHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(ScenarioConfigHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(DeleteRunwayHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(UpdateRunwayHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateFlightsHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(DeleteScenarioHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateWeatherIntervalsHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(WeatherIntervalsHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(FlightHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GetAllDataScenarioConfigHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(DeleteAirportHandler).Assembly);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Maps all Minimal API endpoints (MapPost/MapGet) in one place
Api.Endpoints.MapAll(app);

app.Run();
