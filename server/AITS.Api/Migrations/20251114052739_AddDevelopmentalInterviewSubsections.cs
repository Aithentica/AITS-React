using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDevelopmentalInterviewSubsections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PatientInformationTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { 19, "DEVELOPMENTAL_INTERVIEW_EARLY_EXPERIENCES", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 31, true, "Wcześne doświadczenia" },
                    { 20, "DEVELOPMENTAL_INTERVIEW_ADOLESCENCE", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 32, true, "Adolescencja" },
                    { 21, "DEVELOPMENTAL_INTERVIEW_ADULTHOOD_GENERAL", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 33, true, "Dorosłość - Pytania ogólne" },
                    { 22, "DEVELOPMENTAL_INTERVIEW_ADULTHOOD_PERSONALITY", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 34, true, "Dorosłość - Pytania w kierunku cech nieprawidłowej osobowości" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "PatientInformationTypes",
                keyColumn: "Id",
                keyValue: 22);
        }
    }
}
