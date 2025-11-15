using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddConceptualizationSubsections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PatientInformationTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { 9, "CONCEPTUALIZATION_LEVEL1", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 61, true, "Poziom 1: \"Jak?\" (mapa procesów)" },
                    { 10, "CONCEPTUALIZATION_LEVEL2", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 62, true, "Poziom 2: \"Dlaczego?\" (mechanizmy podtrzymujące)" },
                    { 11, "CONCEPTUALIZATION_SUMMARY", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 63, true, "Podsumowanie: \"Co zmieniamy?\" (cele zmiany)" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 11);
        }
    }
}
