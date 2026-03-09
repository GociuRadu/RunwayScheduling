using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "airports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StandCapacity = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "runways",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AirportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RunwayType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_runways", x => x.Id);
                    table.ForeignKey(
                        name: "FK_runways_airports_AirportId",
                        column: x => x.AirportId,
                        principalTable: "airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scenario_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AirportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Seed = table.Column<int>(type: "integer", nullable: false),
                    AircraftCount = table.Column<int>(type: "integer", nullable: false),
                    AircraftDifficulty = table.Column<int>(type: "integer", nullable: false),
                    OnGroundAircraftCount = table.Column<int>(type: "integer", nullable: false),
                    InboundAircraftCount = table.Column<int>(type: "integer", nullable: false),
                    RemainingOnGroundAircraftCount = table.Column<int>(type: "integer", nullable: false),
                    BaseSeparationSeconds = table.Column<int>(type: "integer", nullable: false),
                    WakePercent = table.Column<int>(type: "integer", nullable: false),
                    WeatherPercent = table.Column<int>(type: "integer", nullable: false),
                    WeatherIntervalCount = table.Column<int>(type: "integer", nullable: false),
                    MinWeatherIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    WeatherDifficulty = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenario_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scenario_configs_airports_AirportId",
                        column: x => x.AirportId,
                        principalTable: "airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aircrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    TailNumber = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    MaxPassengers = table.Column<int>(type: "integer", nullable: false),
                    WakeCategory = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aircrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_aircrafts_scenario_configs_ScenarioConfigId",
                        column: x => x.ScenarioConfigId,
                        principalTable: "scenario_configs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weather_intervals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WeatherType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weather_intervals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weather_intervals_scenario_configs_ScenarioConfigId",
                        column: x => x.ScenarioConfigId,
                        principalTable: "scenario_configs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "flights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    AircraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    Callsign = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxDelayMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaxEarlyMinutes = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_flights_aircrafts_AircraftId",
                        column: x => x.AircraftId,
                        principalTable: "aircrafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_flights_scenario_configs_ScenarioConfigId",
                        column: x => x.ScenarioConfigId,
                        principalTable: "scenario_configs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aircrafts_ScenarioConfigId",
                table: "aircrafts",
                column: "ScenarioConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_flights_AircraftId",
                table: "flights",
                column: "AircraftId");

            migrationBuilder.CreateIndex(
                name: "IX_flights_ScenarioConfigId",
                table: "flights",
                column: "ScenarioConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_flights_ScenarioConfigId_Callsign",
                table: "flights",
                columns: new[] { "ScenarioConfigId", "Callsign" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_runways_AirportId",
                table: "runways",
                column: "AirportId");

            migrationBuilder.CreateIndex(
                name: "IX_scenario_configs_AirportId",
                table: "scenario_configs",
                column: "AirportId");

            migrationBuilder.CreateIndex(
                name: "IX_weather_intervals_ScenarioConfigId",
                table: "weather_intervals",
                column: "ScenarioConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "flights");

            migrationBuilder.DropTable(
                name: "runways");

            migrationBuilder.DropTable(
                name: "weather_intervals");

            migrationBuilder.DropTable(
                name: "aircrafts");

            migrationBuilder.DropTable(
                name: "scenario_configs");

            migrationBuilder.DropTable(
                name: "airports");
        }
    }
}
