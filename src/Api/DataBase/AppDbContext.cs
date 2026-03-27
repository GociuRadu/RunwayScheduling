using Microsoft.EntityFrameworkCore;
using Modules.Airports.Domain;
using Modules.Scenarios.Domain;
using Modules.Aircrafts.Domain;
using Modules.Login.Domain;

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
    public DbSet<User> Users => Set<User>();
    public DbSet<RandomEvent> RandomEvents => Set<RandomEvent>();

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

            e.HasOne<Airport>()
                .WithMany()
                .HasForeignKey(x => x.AirportId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Aircrafts)
                .WithOne()
                .HasForeignKey(a => a.ScenarioConfigId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Flights)
                .WithOne()
                .HasForeignKey(f => f.ScenarioConfigId)
                .OnDelete(DeleteBehavior.Cascade);

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

        modelBuilder.Entity<RandomEvent>(e =>
{
    e.ToTable("random_events");

    e.HasKey(x => x.Id);

    e.Property(x => x.Id)
        .IsRequired();

    e.Property(x => x.ScenarioConfigId)
        .IsRequired();

    e.Property(x => x.Name)
        .IsRequired()
        .HasMaxLength(200);

    e.Property(x => x.Description)
        .HasMaxLength(1500);

    e.Property(x => x.StartTime)
        .IsRequired();

    e.Property(x => x.EndTime)
        .IsRequired();

    e.Property(x => x.ImpactPercent)
        .IsRequired();

    e.HasOne<ScenarioConfig>()
        .WithMany()
        .HasForeignKey(x => x.ScenarioConfigId)
        .OnDelete(DeleteBehavior.Cascade);
});

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);

            e.Property(x => x.Email)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.Username)
                .HasMaxLength(50)
                .IsRequired();

            e.Property(x => x.PasswordHash)
                .HasMaxLength(300)
                .IsRequired();

            e.Property(x => x.CreatedAtUtc).IsRequired();

            e.HasIndex(x => x.Email).IsUnique();

            // TODO: SECURITY: This seeds deterministic admin accounts with plaintext passwords in source control. Replace with environment-provisioned bootstrap users or pre-hashed secrets stored outside the repository.
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Email = "admin1@gmail.com",
                    Username = "admin1",
                    PasswordHash = "Admin1234",
                    CreatedAtUtc = new DateTime(2026, 3, 9, 2, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Email = "admin2@gmail.com",
                    Username = "admin2",
                    PasswordHash = "Admin1234",
                    CreatedAtUtc = new DateTime(2026, 3, 9, 3, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Email = "admin3@gmail.com",
                    Username = "admin3",
                    PasswordHash = "Admin1234",
                    CreatedAtUtc = new DateTime(2026, 3, 9, 4, 0, 0, DateTimeKind.Utc)
                });
        });
    }
}
