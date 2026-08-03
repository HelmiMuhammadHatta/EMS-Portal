using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentCandidateModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CandidateId",
                table: "Employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    AppliedDepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppliedPositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Candidates_Departments_AppliedDepartmentId",
                        column: x => x.AppliedDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Candidates_Positions_AppliedPositionId",
                        column: x => x.AppliedPositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CandidateId",
                table: "Employees",
                column: "CandidateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_AppliedDepartmentId",
                table: "Candidates",
                column: "AppliedDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_AppliedPositionId",
                table: "Candidates",
                column: "AppliedPositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Candidates_CandidateId",
                table: "Employees",
                column: "CandidateId",
                principalTable: "Candidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Candidates_CandidateId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "Candidates");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CandidateId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CandidateId",
                table: "Employees");
        }
    }
}
