using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TestId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionText = table.Column<string>(type: "text", nullable: false),
                    QuestionOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestQuestions_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TestId = table.Column<Guid>(type: "uuid", nullable: false),
                    TakenByType = table.Column<int>(type: "integer", nullable: false),
                    TakenById = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestSessions_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TestQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionText = table.Column<string>(type: "text", nullable: false),
                    OptionOrder = table.Column<int>(type: "integer", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    TraitCategory = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestQuestionOptions_TestQuestions_TestQuestionId",
                        column: x => x.TestQuestionId,
                        principalTable: "TestQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TestSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestType = table.Column<int>(type: "integer", nullable: false),
                    TotalQuestions = table.Column<int>(type: "integer", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "integer", nullable: false),
                    ScorePercentage = table.Column<double>(type: "double precision", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    ScoreD = table.Column<int>(type: "integer", nullable: false),
                    ScoreI = table.Column<int>(type: "integer", nullable: false),
                    ScoreS = table.Column<int>(type: "integer", nullable: false),
                    ScoreC = table.Column<int>(type: "integer", nullable: false),
                    DominantTrait = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestResults_TestSessions_TestSessionId",
                        column: x => x.TestSessionId,
                        principalTable: "TestSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TestSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedOptionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestAnswers_TestQuestionOptions_SelectedOptionId",
                        column: x => x.SelectedOptionId,
                        principalTable: "TestQuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestAnswers_TestQuestions_TestQuestionId",
                        column: x => x.TestQuestionId,
                        principalTable: "TestQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestAnswers_TestSessions_TestSessionId",
                        column: x => x.TestSessionId,
                        principalTable: "TestSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestAnswers_SelectedOptionId",
                table: "TestAnswers",
                column: "SelectedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestAnswers_TestQuestionId",
                table: "TestAnswers",
                column: "TestQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestAnswers_TestSessionId",
                table: "TestAnswers",
                column: "TestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestionOptions_TestQuestionId",
                table: "TestQuestionOptions",
                column: "TestQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestions_TestId",
                table: "TestQuestions",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestSessionId",
                table: "TestResults",
                column: "TestSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestSessions_TestId",
                table: "TestSessions",
                column: "TestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestAnswers");

            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "TestQuestionOptions");

            migrationBuilder.DropTable(
                name: "TestSessions");

            migrationBuilder.DropTable(
                name: "TestQuestions");

            migrationBuilder.DropTable(
                name: "Tests");
        }
    }
}
