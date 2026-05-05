using Api;
using Api.Authentication;
using Api.DataBase;
using Api.Errors;
using Api.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Modules.Aircrafts.Application;
using Modules.Aircrafts.Application.UseCases.GenerateRandomAircraft;
using Modules.Airports.Application;
using Modules.Airports.Application.UseCases.CreateAirport;
using Modules.Login.Application;
using Modules.Login.Application.UseCases.Login;
using Modules.Scenarios.Application;
using Modules.Scenarios.Application.Services;
using Modules.Scenarios.Application.UseCases.CreateScenarioConfig;
using Modules.Scenarios.Application.UseCases.CreateFlights;
using Modules.Solver.Application;
using Modules.Solver.Application.Scheduling;
using Modules.Solver.Application.Snapshot;
using Modules.Solver.Application.UseCases.SolveGreedy;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

AddDatabase(builder.Services, builder.Configuration);
AddJwtAuthentication(builder.Services, builder.Configuration);
AddApiProblemDetails(builder.Services);
AddOpenApiDocumentation(builder.Services);
AddApplicationServices(builder.Services);
AddMediatorHandlers(builder.Services);
AddApiPolicies(builder.Services);
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// catches unhandled exceptions and returns problem details
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// handles error responses that have no body (e.g. 401 from auth middleware directly)
app.UseStatusCodePages(async context =>
{
    var statusCode = context.HttpContext.Response.StatusCode;
    if (statusCode < StatusCodes.Status400BadRequest || statusCode == StatusCodes.Status204NoContent)
        return;

    var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
    await problemDetailsService.WriteAsync(new ProblemDetailsContext
    {
        HttpContext = context.HttpContext,
        ProblemDetails = ProblemDetailsDefaults.Create(statusCode)
    });
});

app.UseCors("frontend");
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

Endpoints.MapAll(app);
app.MapHealthChecks("/health").AllowAnonymous();

// applies pending ef migrations at startup so the db is always in sync with the models
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.Run();

void AddDatabase(IServiceCollection services, IConfiguration configuration)
{
    // registers AppDbContext in DI and connects it to PostgreSQL using the connection string from config
    services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("Default")));
}

void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
{
    // validates JWT config values at startup so the app fails fast if something is missing or wrong
    services.AddOptions<JwtOptions>()
        .Bind(configuration.GetRequiredSection(JwtOptions.SectionName))
        .ValidateDataAnnotations()
        .Validate(options => options.Key.Length >= 32, "JWT signing key must contain at least 32 characters.")
        .ValidateOnStart();

    // creates the JwtOptions object with the actual values so we can use them in AddJwtBearer below
    var jwtOptions = configuration.GetRequiredSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException("JWT configuration is missing.");

    // tells ASP.NET how to validate incoming bearer tokens on every request
    services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    services.AddAuthorization();
}

void AddApiProblemDetails(IServiceCollection services)
{
    // adds request path and trace ID to every error response for easier debugging
    services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance = context.HttpContext.Request.Path;
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            if (context.ProblemDetails.Status is { } statusCode)
            {
                var defaults = ProblemDetailsDefaults.Create(statusCode);
                context.ProblemDetails.Title ??= defaults.Title;
                context.ProblemDetails.Detail ??= defaults.Detail;
            }
        };
    });
}

void AddOpenApiDocumentation(IServiceCollection services)
{
    // sets up swagger UI with JWT authorize 
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(options =>
    {
        const string schemeId = "Bearer";

        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Api", Version = "v1" });

        // adds the Authorize button in Swagger UI so you can test protected endpoints
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
}

void AddApplicationServices(IServiceCollection services)
{
    // registers all stores, services and factories in DI
    services.AddScoped<IAirportStore, EfAirportStore>();
    services.AddScoped<IRunwayStore, EfRunwayStore>();
    services.AddScoped<IScenarioConfigStore, EfScenarioConfigStore>();
    services.AddScoped<IAircraftStore, EfAircraftStore>();
    services.AddScoped<IFlightStore, EfFlightStore>();
    services.AddScoped<IWeatherIntervalStore, EfWeatherIntervalStore>();
    services.AddScoped<IUserStore, EfUserStore>();
    services.AddScoped<ITokenService, JwtTokenService>();
    services.AddScoped<IRandomEventStore, EfRandomEventStore>();
    services.AddScoped<IScenarioSnapshotFactory, ScenarioSnapshotFactory>();
    services.AddScoped<IBenchmarkEntryStore, EfBenchmarkEntryStore>();
    services.AddSingleton<ISchedulingEngine, SchedulingEngine>();
    services.AddScoped<FlightScheduler>();
}

void AddMediatorHandlers(IServiceCollection services)
{
    // registers all MediatR handlers from every module and adds validation as a pipeline step
    services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblies(
            typeof(GenerateRandomAircraftHandler).Assembly,
            typeof(CreateAirportHandler).Assembly,
            typeof(CreateScenarioConfigHandler).Assembly,
            typeof(LoginHandler).Assembly,
            typeof(SolveGreedyHandler).Assembly);
        cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    });

    services.AddValidatorsFromAssemblyContaining<CreateFlightsCommandValidator>();
    services.AddValidatorsFromAssemblyContaining<CreateAirportCommandValidator>();
}

void AddApiPolicies(IServiceCollection services)
{
    // limits login to 5 requests per minute to prevent brute force
    services.AddRateLimiter(options =>
    {
        options.AddSlidingWindowLimiter("login", limiterOptions =>
        {
            limiterOptions.PermitLimit = 5;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.SegmentsPerWindow = 6;
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // allows the frontend dev server to make requests to this API
    services.AddCors(options =>
    {
        options.AddPolicy("frontend", policy =>
            policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}
