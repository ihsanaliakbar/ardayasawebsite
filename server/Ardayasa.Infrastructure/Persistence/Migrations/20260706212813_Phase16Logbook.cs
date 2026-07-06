using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ardayasa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase16Logbook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogbookEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorPsychologistId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SessionNumber = table.Column<int>(type: "integer", nullable: false),
                    CaseSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SessionActivities = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Homework = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    NextSessionPlan = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    FollowUpNeeded = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogbookEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogbookEntries_AspNetUsers_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogbookEntries_Psychologists_AuthorPsychologistId",
                        column: x => x.AuthorPsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogbookEntries_AuthorPsychologistId",
                table: "LogbookEntries",
                column: "AuthorPsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_LogbookEntries_PatientUserId",
                table: "LogbookEntries",
                column: "PatientUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogbookEntries");
        }
    }
}
