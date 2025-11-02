using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientDemographicData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime?>(
                name: "DateOfBirth",
                table: "Patient",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Patient",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pesel",
                table: "Patient",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Patient",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StreetNumber",
                table: "Patient",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApartmentNumber",
                table: "Patient",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Patient",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Patient",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Patient",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Pesel",
                table: "Patient",
                column: "Pesel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patient_Pesel",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "Pesel",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "StreetNumber",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "ApartmentNumber",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Patient");
        }
    }
}
