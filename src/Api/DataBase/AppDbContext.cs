using Microsoft.EntityFrameworkCore;
using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Aircrafts.Domain;

namespace Api.DataBase;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Runway> Runways => Set<Runway>();
    public DbSet<ScenarioConfig> ScenarioConfigs => Set<ScenarioConfig>();
    public DbSet<WeatherInterval> WeatherIntervals => Set<WeatherInterval>();
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<Aircraft> Aircrafts => Set<Aircraft>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Airport>(e =>
        {
            e.ToTable("airports");
            e.HasKey(x => x.Id);

            e.Property(x => x.StandCapacity).IsRequired();
            e.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.Latitude).IsRequired();
            e.Property(x => x.Longitude).IsRequired();
        });

        modelBuilder.Entity<Runway>(e =>
        {
            e.ToTable("runways");
            e.HasKey(x => x.Id);

            e.Property(x => x.Name)
                .HasMaxLength(50)
                .IsRequired();

            e.Property(x => x.IsActive).IsRequired();
            e.Property(x => x.RunwayType).IsRequired();
            e.Property(x => x.AirportId).IsRequired();

            e.HasOne<Airport>()
                .WithMany()
                .HasForeignKey(x => x.AirportId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.AirportId);
        });

        modelBuilder.Entity<ScenarioConfig>(e =>
        {
            e.ToTable("scenario_configs");
            e.HasKey(x => x.Id);

            e.Property(x => x.AirportId).IsRequired();

            e.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.Difficulty).IsRequired();
            e.Property(x => x.StartTime).IsRequired();
            e.Property(x => x.EndTime).IsRequired();
            e.Property(x => x.Seed).IsRequired();

            e.Property(x => x.AircraftCount).IsRequired();
            e.Property(x => x.AircraftDifficulty).IsRequired();
            e.Property(x => x.OnGroundAircraftCount).IsRequired();
            e.Property(x => x.InboundAircraftCount).IsRequired();
            e.Property(x => x.RemainingOnGroundAircraftCount).IsRequired();
            e.Property(x => x.BaseSeparationSeconds).IsRequired();
            e.Property(x => x.WakePercent).IsRequired();
            e.Property(x => x.WeatherPercent).IsRequired();
            e.Property(x => x.WeatherIntervalCount).IsRequired();
            e.Property(x => x.MinWeatherIntervalMinutes).IsRequired();
            e.Property(x => x.WeatherDifficulty).IsRequired();

            // ScenarioConfig -> Airport
            e.HasOne<Airport>()
                .WithMany()
                .HasForeignKey(x => x.AirportId)
                .OnDelete(DeleteBehavior.Cascade);

            // ScenarioConfig -> Aircrafts (CASCADE)
            e.HasMany(x => x.Aircrafts)
                .WithOne()
                .HasForeignKey(a => a.ScenarioConfigId)
                .OnDelete(DeleteBehavior.Cascade);

            // ScenarioConfig -> Flights (CASCADE)
            e.HasMany(x => x.Flights)
                .WithOne()
                .HasForeignKey(f => f.ScenarioConfigId)
                .OnDelete(DeleteBehavior.Cascade);

            // ScenarioConfig -> WeatherIntervals (CASCADE)
            e.HasMany(x => x.WeatherIntervals)
                .WithOne()
                .HasForeignKey(w => w.ScenarioConfigId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.AirportId);
        });

        modelBuilder.Entity<Aircraft>(e =>
        {
            e.ToTable("aircrafts");
            e.HasKey(x => x.Id);

            e.Property(x => x.ScenarioConfigId).IsRequired();
            e.HasIndex(x => x.ScenarioConfigId);

        });

        modelBuilder.Entity<Flight>(e =>
        {
            e.ToTable("flights");
            e.HasKey(x => x.Id);

            e.Property(x => x.ScenarioConfigId).IsRequired();
            e.Property(x => x.AircraftId).IsRequired();

            e.Property(x => x.Callsign)
                .HasMaxLength(32)
                .IsRequired();

            e.Property(x => x.Type).IsRequired();
            e.Property(x => x.ScheduledTime).IsRequired();
            e.Property(x => x.MaxDelayMinutes).IsRequired();
            e.Property(x => x.MaxEarlyMinutes).IsRequired();
            e.Property(x => x.Priority).IsRequired();

            // Flight -> Aircraft (no cascade )
            e.HasOne<Aircraft>()
                .WithMany()
                .HasForeignKey(x => x.AircraftId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.ScenarioConfigId);
            e.HasIndex(x => new { x.ScenarioConfigId, x.Callsign }).IsUnique();
            e.HasIndex(x => x.AircraftId);
        });

        modelBuilder.Entity<WeatherInterval>(e =>
        {
            e.ToTable("weather_intervals");
            e.HasKey(x => x.Id);

            e.Property(x => x.ScenarioConfigId).IsRequired();
            e.Property(x => x.StartTime).IsRequired();
            e.Property(x => x.EndTime).IsRequired();
            e.Property(x => x.WeatherType).IsRequired();

            e.HasIndex(x => x.ScenarioConfigId);
        });
    }
}