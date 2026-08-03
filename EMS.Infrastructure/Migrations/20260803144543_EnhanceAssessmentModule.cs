using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceAssessmentModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessCode",
                table: "TestSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedAccessAttempts",
                table: "TestSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "TestSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LogicScorePercentage",
                table: "TestResults",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NumericScorePercentage",
                table: "TestResults",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "VerbalScorePercentage",
                table: "TestResults",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "TestQuestions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProctoringSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TestSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProctoringSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProctoringSnapshots_TestSessions_TestSessionId",
                        column: x => x.TestSessionId,
                        principalTable: "TestSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProctoringSnapshots_TestSessionId",
                table: "ProctoringSnapshots",
                column: "TestSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProctoringSnapshots");

            migrationBuilder.DropColumn(
                name: "AccessCode",
                table: "TestSessions");

            migrationBuilder.DropColumn(
                name: "FailedAccessAttempts",
                table: "TestSessions");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "TestSessions");

            migrationBuilder.DropColumn(
                name: "LogicScorePercentage",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "NumericScorePercentage",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "VerbalScorePercentage",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "TestQuestions");
        }
    }
}
