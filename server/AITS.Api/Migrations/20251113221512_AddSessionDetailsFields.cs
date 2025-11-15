using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionDetailsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgreedPersonalWork",
                table: "Session",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalWorkDiscussion",
                table: "Session",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousSessionReflections",
                table: "Session",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousWeekEvents",
                table: "Session",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionSummary",
                table: "Session",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TherapeuticIntervention",
                table: "Session",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgreedPersonalWork",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "PersonalWorkDiscussion",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "PreviousSessionReflections",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "PreviousWeekEvents",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "SessionSummary",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "TherapeuticIntervention",
                table: "Session");
        }
    }
}
