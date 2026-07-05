using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ardayasa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase15PatientIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PsychologistId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAssignments_AspNetUsers_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientAssignments_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BirthPlace = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DomicileAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MaritalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LastEducation = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Occupation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HasAccessedPsychologyServices = table.Column<bool>(type: "boolean", nullable: true),
                    HasPriorDiagnosis = table.Column<bool>(type: "boolean", nullable: true),
                    PriorDiagnosis = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ConsultationConcerns = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CounselingExpectations = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_PatientProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAssignments_PatientUserId_PsychologistId",
                table: "PatientAssignments",
                columns: new[] { "PatientUserId", "PsychologistId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientAssignments_PsychologistId",
                table: "PatientAssignments",
                column: "PsychologistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientAssignments");

            migrationBuilder.DropTable(
                name: "PatientProfiles");
        }
    }
}
