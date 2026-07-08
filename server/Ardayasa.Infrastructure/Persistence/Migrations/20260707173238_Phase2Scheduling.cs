using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ardayasa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2Scheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AvailabilityExceptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PsychologistId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvailabilityExceptions_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AvailabilityRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PsychologistId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvailabilityRules_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PsychologistId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    PriceIdr = table.Column<decimal>(type: "numeric(12,0)", precision: 12, scale: 0, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ZoomLink = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentDueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StatusChangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_AspNetUsers_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "PsychologistServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PsychologistId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychologistServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PsychologistServices_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PsychologistServices_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityExceptions_PsychologistId_Date",
                table: "AvailabilityExceptions",
                columns: new[] { "PsychologistId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityRules_PsychologistId",
                table: "AvailabilityRules",
                column: "PsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ActiveSlot",
                table: "Bookings",
                columns: new[] { "PsychologistId", "StartUtc" },
                unique: true,
                filter: "\"Status\" IN ('PendingPayment', 'AwaitingVerification', 'Confirmed')");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PatientUserId",
                table: "Bookings",
                column: "PatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Psychologist_Start",
                table: "Bookings",
                columns: new[] { "PsychologistId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ServiceId",
                table: "Bookings",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PsychologistServices_PsychologistId_ServiceId",
                table: "PsychologistServices",
                columns: new[] { "PsychologistId", "ServiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsychologistServices_ServiceId",
                table: "PsychologistServices",
                column: "ServiceId");

            // Range-based double-booking guard: the partial unique index above only
            // catches identical starts; this rejects ANY overlapping active bookings
            // for a psychologist, even across different durations. Postgres-only
            // (tests run on SQLite via EnsureCreated and rely on the unique index +
            // the app-level slot check).
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql("""
                    CREATE EXTENSION IF NOT EXISTS btree_gist;

                    ALTER TABLE "Bookings" ADD CONSTRAINT "EX_Bookings_NoActiveOverlap"
                    EXCLUDE USING gist (
                        "PsychologistId" WITH =,
                        tstzrange("StartUtc", "EndUtc") WITH &&
                    )
                    WHERE ("Status" IN ('PendingPayment', 'AwaitingVerification', 'Confirmed'));
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailabilityExceptions");

            migrationBuilder.DropTable(
                name: "AvailabilityRules");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "ClinicSettings");

            migrationBuilder.DropTable(
                name: "PsychologistServices");
        }
    }
}
