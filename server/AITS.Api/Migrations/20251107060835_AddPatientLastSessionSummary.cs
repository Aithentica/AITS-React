using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientLastSessionSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastSessionSummary",
                table: "Patient",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Translations",
                columns: new[] { "Id", "Culture", "Key", "Value" },
                values: new object[,]
                {
                    { 193, "pl", "common.logout", "Wyloguj" },
                    { 194, "en", "common.logout", "Log out" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 193);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 194);

            migrationBuilder.DropColumn(
                name: "LastSessionSummary",
                table: "Patient");
        }
    }
}
