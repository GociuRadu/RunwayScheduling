using System.Text;
using System.Threading.RateLimiting;
using Api.DataBase;
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
using Modules.Solver.Application.GeneticAlgorithmSolver;
using Modules.Solver.Application.GreedySolver;

namespace Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddJwtAuthentication(configuration);
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
        var jwtKey = configuration["JWT:KEY"]!;
        var jwtIssuer = configuration["JWT:ISSUER"]!;
        var jwtAudience = configuration["JWT:AUDIENCE"]!;

        services
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

        services.AddAuthorization();
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
        services.AddScoped<IWeatherIntervalStore, EFWeatherIntervalStore>();
        services.AddScoped<IUserStore, EfUserStore>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IRandomEventStore, EFRandomEvent>();
        services.AddScoped<IScenarioSnapshotLoader, ScenarioSnapshotLoader>();
        services.AddScoped<GreedyScenarioSolver>();
        services.AddScoped<GeneticAlgorithmScenarioSolver>();
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
                typeof(GreedySolverHandler).Assembly);
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
