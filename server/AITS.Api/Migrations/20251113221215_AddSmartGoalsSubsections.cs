using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartGoalsSubsections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PatientInformationTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { 12, "SMART_GOALS_CONNECTIONS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 81, true, "Powiązania" },
                    { 13, "SMART_GOALS_DEFINITION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 82, true, "Definicja SMART" },
                    { 14, "SMART_GOALS_METRICS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 83, true, "Metryka i monitoring" },
                    { 15, "SMART_GOALS_ACTION_PLAN", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 84, true, "Plan działania" },
                    { 16, "SMART_GOALS_BARRIERS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 85, true, "Bariery i wsparcie" },
                    { 17, "SMART_GOALS_REVIEW", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 86, true, "Przegląd i weryfikacja" },
                    { 18, "SMART_GOALS_PRIORITY", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 87, true, "Priorytet" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 18);
        }
    }
}
