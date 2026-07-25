using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftRotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsManualOverride",
                table: "ShiftSchedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RotationGroupId",
                table: "Employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShiftRotationGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RotationStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftRotationGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShiftRotationPatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    RotationGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleWeekNumber = table.Column<int>(type: "integer", nullable: false),
                    WorkShiftId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftRotationPatterns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftRotationPatterns_ShiftRotationGroups_RotationGroupId",
                        column: x => x.RotationGroupId,
                        principalTable: "ShiftRotationGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftRotationPatterns_WorkShifts_WorkShiftId",
                        column: x => x.WorkShiftId,
                        principalTable: "WorkShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_RotationGroupId",
                table: "Employees",
                column: "RotationGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftRotationPatterns_RotationGroupId_CycleWeekNumber",
                table: "ShiftRotationPatterns",
                columns: new[] { "RotationGroupId", "CycleWeekNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftRotationPatterns_WorkShiftId",
                table: "ShiftRotationPatterns",
                column: "WorkShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_ShiftRotationGroups_RotationGroupId",
                table: "Employees",
                column: "RotationGroupId",
                principalTable: "ShiftRotationGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_ShiftRotationGroups_RotationGroupId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "ShiftRotationPatterns");

            migrationBuilder.DropTable(
                name: "ShiftRotationGroups");

            migrationBuilder.DropIndex(
                name: "IX_Employees_RotationGroupId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsManualOverride",
                table: "ShiftSchedules");

            migrationBuilder.DropColumn(
                name: "RotationGroupId",
                table: "Employees");
        }
    }
}
