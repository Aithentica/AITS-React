using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientInformationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientInformationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientInformationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatientInformationEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    PatientInformationTypeId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientInformationEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientInformationEntries_Patient_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patient",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientInformationEntries_PatientInformationTypes_PatientInformationTypeId",
                        column: x => x.PatientInformationTypeId,
                        principalTable: "PatientInformationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientInformationTypes_Code",
                table: "PatientInformationTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientInformationEntries_PatientInformationTypeId",
                table: "PatientInformationEntries",
                column: "PatientInformationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientInformationEntries_PatientId_PatientInformationTypeId",
                table: "PatientInformationEntries",
                columns: new[] { "PatientId", "PatientInformationTypeId" },
                unique: true);

            migrationBuilder.InsertData(
                table: "PatientInformationTypes",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "INITIAL_CONSULTATION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, true, "Konsultacja wstępna" },
                    { 2, "DEMOGRAPHIC_INTERVIEW", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2, true, "Wywiad demograficzny" },
                    { 3, "DEVELOPMENTAL_INTERVIEW", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3, true, "Wywiad rozwojowy" },
                    { 4, "PROBLEM_IDENTIFICATION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4, true, "Określenie problemu (Co?)" },
                    { 5, "PROBLEM_DESCRIPTION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 5, true, "Opis Problemu (Co?) Szczegółowo" },
                    { 6, "CONCEPTUALIZATION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, true, "Konceptualizacja" },
                    { 7, "STANDARD_SESSION", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 7, true, "Sesja standardowa" },
                    { 8, "SMART_GOALS", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 8, true, "Cele SMART" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientInformationEntries");

            migrationBuilder.DropTable(
                name: "PatientInformationTypes");
        }
    }
}
