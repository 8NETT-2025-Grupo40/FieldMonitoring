using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldMonitoring.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration

    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fields",
                columns: table => new
                {
                    FieldId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FarmId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SensorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", maxLength: 50, nullable: false),
                    StatusReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastReadingAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSoilMoisture = table.Column<double>(type: "float", nullable: true),
                    LastSoilTemperature = table.Column<double>(type: "float", nullable: true),
                    LastAirTemperature = table.Column<double>(type: "float", nullable: true),
                    LastAirHumidity = table.Column<double>(type: "float", nullable: true),
                    LastRain = table.Column<double>(type: "float", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastTimeAboveDryThreshold = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTimeBelowHeatThreshold = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTimeAboveFrostThreshold = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTimeAboveDryAirThreshold = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTimeBelowHumidAirThreshold = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fields", x => x.FieldId);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedReadings",
                columns: table => new
                {
                    ReadingId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FieldId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedReadings", x => x.ReadingId);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    AlertId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FarmId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.AlertId);
                    table.ForeignKey(
                        name: "FK_Alerts_Fields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "Fields",
                        principalColumn: "FieldId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_FarmId_Status",
                table: "Alerts",
                columns: new[] { "FarmId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_FieldId_AlertType_Status",
                table: "Alerts",
                columns: new[] { "FieldId", "AlertType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_FieldId_StartedAt",
                table: "Alerts",
                columns: new[] { "FieldId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Fields_FarmId",
                table: "Fields",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_Fields_FarmId_Status",
                table: "Fields",
                columns: new[] { "FarmId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Fields_Status",
                table: "Fields",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedReadings_FieldId",
                table: "ProcessedReadings",
                column: "FieldId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "ProcessedReadings");

            migrationBuilder.DropTable(
                name: "Fields");
        }
    }
}
