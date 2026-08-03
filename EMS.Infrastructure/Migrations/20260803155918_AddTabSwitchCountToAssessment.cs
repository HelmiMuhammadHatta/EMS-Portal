using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTabSwitchCountToAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TabSwitchCount",
                table: "TestSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TabSwitchCount",
                table: "TestResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TabSwitchCount",
                table: "TestSessions");

            migrationBuilder.DropColumn(
                name: "TabSwitchCount",
                table: "TestResults");
        }
    }
}
