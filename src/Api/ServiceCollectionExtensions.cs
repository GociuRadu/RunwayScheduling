using Api.Authentication;
using System.Text;
using System.Threading.RateLimiting;
using Api.DataBase;
using Api.Errors;
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
using Modules.Scenarios.Application.UseCases.CreateScenarioConfig;
using Modules.Solver.Application;
using Modules.Solver.Application.Scheduling;
using Modules.Solver.Application.Snapshot;
using Modules.Solver.Application.UseCases.SolveGreedy;

namespace Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddJwtAuthentication(configuration);
        services.AddApiProblemDetails();
        services.AddOpenApiDocumentation();
        services.AddApplicationServices();
        services.AddMediatorHandlers();
        services.AddApiPolicies();

        return services;
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));
    }

    private static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetRequiredSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => options.Key.Length >= 32, "JWT signing key must contain at least 32 characters.")
            .ValidateOnStart();

        var jwtOptions = configuration.GetRequiredSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");

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

    private static void AddApiProblemDetails(this IServiceCollection services)
    {
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

    private static void AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
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
    }

    private static void AddApplicationServices(this IServiceCollection services)
    {
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
    }

    private static void AddMediatorHandlers(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                typeof(GenerateRandomAircraftHandler).Assembly,
                typeof(CreateAirportHandler).Assembly,
                typeof(CreateScenarioConfigHandler).Assembly,
                typeof(LoginHandler).Assembly,
                typeof(SolveGreedyHandler).Assembly);
        });
    }

    private static void AddApiPolicies(this IServiceCollection services)
    {
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

        services.AddCors(options =>
        {
            options.AddPolicy("frontend", policy =>
                policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });
    }
}
