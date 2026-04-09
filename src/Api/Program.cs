using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Api.DataBase;

using Modules.Aircrafts.Application;
using Modules.Aircrafts.Application.UseCases.GenerateRandomAircraft;

using Modules.Airports.Application;
using Modules.Airports.Application.UseCases.CreateAirport;

using Modules.Scenarios.Application;
using Modules.Scenarios.Application.UseCases.CreateScenarioConfig;

using Modules.Login.Application;
using Modules.Login.Application.UseCases.Login;

using Modules.Solver.Application;
using Modules.Solver.Application.GreedySolver;
using Modules.Solver.Application.GeneticAlgorithmSolver;


var builder = WebApplication.CreateBuilder(args);



// DB PostgreSQL
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));



//JWT
var jwtKey = builder.Configuration["JWT:KEY"]!;
var jwtIssuer = builder.Configuration["JWT:ISSUER"]!;
var jwtAudience = builder.Configuration["JWT:AUDIENCE"]!;

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    const string schemeId = "Bearer";

    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Api", Version = "v1" });

    options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(schemeId, document)] = []
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
builder.Services.AddScoped<GeneticAlgorithmScenarioSolver>();

// MediatR — one assembly registration per module
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(GenerateRandomAircraftHandler).Assembly, // Modules.Aircrafts.Application
        typeof(CreateAirportHandler).Assembly,          // Modules.Airports.Application
        typeof(CreateScenarioConfigHandler).Assembly,   // Modules.Scenarios.Application
        typeof(LoginHandler).Assembly,                  // Modules.Login.Application
        typeof(GreedySolverHandler).Assembly            // Modules.Solver.Application
    );
});


// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 6;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// CORS frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});



var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.UseCors("frontend");
app.UseRateLimiter();
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
