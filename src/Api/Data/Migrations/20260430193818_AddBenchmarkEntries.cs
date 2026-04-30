using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBenchmarkEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "benchmark_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlgorithmType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConfigIndex = table.Column<int>(type: "integer", nullable: false),
                    RunTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fitness = table.Column<double>(type: "double precision", nullable: false),
                    SolveTimeMs = table.Column<double>(type: "double precision", nullable: false),
                    PopulationSize = table.Column<int>(type: "integer", nullable: true),
                    MaxGenerations = table.Column<int>(type: "integer", nullable: true),
                    CrossoverRate = table.Column<double>(type: "double precision", nullable: true),
                    MutationRateLocal = table.Column<double>(type: "double precision", nullable: true),
                    MutationRateMemetic = table.Column<double>(type: "double precision", nullable: true),
                    TournamentSize = table.Column<int>(type: "integer", nullable: true),
                    EliteCount = table.Column<int>(type: "integer", nullable: true),
                    NoImprovementGenerations = table.Column<int>(type: "integer", nullable: true),
                    RandomSeed = table.Column<int>(type: "integer", nullable: true),
                    EnableCpSatRefinement = table.Column<bool>(type: "boolean", nullable: true),
                    CpSatMicroEnabled = table.Column<bool>(type: "boolean", nullable: true),
                    CpSatMicroEveryNGenerations = table.Column<int>(type: "integer", nullable: true),
                    CpSatMacroEnabled = table.Column<bool>(type: "boolean", nullable: true),
                    CpSatMacroEveryNGenerations = table.Column<int>(type: "integer", nullable: true),
                    CpSatEliteCount = table.Column<int>(type: "integer", nullable: true),
                    CpSatRandomCount = table.Column<int>(type: "integer", nullable: true),
                    CpSatMacroWindowCount = table.Column<int>(type: "integer", nullable: true),
                    CpSatTimeLimitMsMicro = table.Column<int>(type: "integer", nullable: true),
                    CpSatTimeLimitMsMacro = table.Column<int>(type: "integer", nullable: true),
                    CpSatNeighborhoodSize = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_benchmark_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_benchmark_entries_scenario_configs_ScenarioConfigId",
                        column: x => x.ScenarioConfigId,
                        principalTable: "scenario_configs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_benchmark_entries_ScenarioConfigId",
                table: "benchmark_entries",
                column: "ScenarioConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_benchmark_entries_ScenarioConfigId_AlgorithmType",
                table: "benchmark_entries",
                columns: new[] { "ScenarioConfigId", "AlgorithmType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "benchmark_entries");
        }
    }
}
