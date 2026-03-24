using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Api.DataBase;

using Modules.Aircrafts.Application;
using Modules.Aircrafts.Application.UseCases.GenerateRandomAircraft;
using Modules.Aircrafts.Application.UseCases.GetAircrafts;

using Modules.Airports.Application;
using Modules.Airports.Application.UseCases.CreateAirport;
using Modules.Airports.Application.UseCases.DeleteAirport;
using Modules.Airports.Application.UseCases.DeleteRunway;
using Modules.Airports.Application.UseCases.GetAirports;
using Modules.Airports.Application.UseCases.GetRunwaysByAirportId;
using Modules.Airports.Application.UseCases.UpdateRunway;

using Modules.Scenarios.Application;
using Modules.Scenarios.Application.UseCases.CreateFlights;
using Modules.Scenarios.Application.UseCases.CreateScenarioConfig;
using Modules.Scenarios.Application.UseCases.CreateWeatherIntervals;
using Modules.Scenarios.Application.UseCases.DeleteScenario;
using Modules.Scenarios.Application.UseCases.GetAllDataScenarioConfig;
using Modules.Scenarios.Application.UseCases.GetFlights;
using Modules.Scenarios.Application.UseCases.GetScenarioConfigs;
using Modules.Scenarios.Application.UseCases.GetWeatherIntervals;
using Modules.Scenarios.Application.UseCases.CreateRandomEvent;
using Modules.Scenarios.Application.UseCases.DeleteRandomEvent;
using Modules.Login.Application;
using Modules.Login.Application.UseCases.Login;
using Modules.Scenarios.Application.UseCases.GetRandomEventsByScenarioConfigId;
using Modules.Scenarios.Application.UseCases.UpdateRandomEvent;
using Modules.Solver.Application.GreedySolver;
using  Modules.Solver.Application;


var builder = WebApplication.CreateBuilder(args);



// DB PostgreSQL
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));



//JWT
var jwtKey = builder.Configuration["JWT:KEY"]!;
var jwtIssuer = builder.Configuration["JWT:ISSUER"]!;
var jwtAudience = builder.Configuration["JWT:AUDIENCE"]!;

//how the user is identified
builder.Services
.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,

        ValidateAudience = true,
        ValidAudience = jwtAudience,

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        ),

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

//what the identified user is allowed to access
builder.Services.AddAuthorization();



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    const string schemeId = "Bearer";

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Api",
        Version = "v1"
    });

    options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(document =>
    {
        return new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(schemeId, document)] = []
        };
    });
});



// Stores
builder.Services.AddScoped<IAirportStore, EfAirportStore>();
builder.Services.AddScoped<IRunwayStore, EfRunwayStore>();
builder.Services.AddScoped<IScenarioConfigStore, EfScenarioConfigStore>();
builder.Services.AddScoped<IAircraftStore, EfAircraftStore>();
builder.Services.AddScoped<IFlightStore, EfFlightStore>();
builder.Services.AddScoped<IWeatherIntervalStore, EFWeatherIntervalStore>();
builder.Services.AddScoped<IUserStore, EfUserStore>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IRandomEventStore, EFRandomEvent>();
builder.Services.AddScoped<IScenarioSnapshotLoader, ScenarioSnapshotLoader>();
builder.Services.AddScoped<GreedyScenarioSolver>();

// MediatR
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
    cfg.RegisterServicesFromAssembly(typeof(LoginHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateRandomEventHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(DeleteRandomEventHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GetRandomEventsByScenarioConfigIdHandler).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(GreedySolverHandler).Assembly);

});


// CORS frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});



var app = builder.Build();



// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



// pipeline
app.UseCors("frontend");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

Api.Endpoints.MapAll(app);



using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();