using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTherapistProfileAndDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TherapistProfiles",
                columns: table => new
                {
                    TherapistId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Regon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BusinessAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BusinessCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BusinessPostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    BusinessCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsCompany = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TherapistProfiles", x => x.TherapistId);
                    table.ForeignKey(
                        name: "FK_TherapistProfiles_AspNetUsers_TherapistId",
                        column: x => x.TherapistId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TherapistDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TherapistId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileContent = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TherapistDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TherapistDocuments_TherapistProfiles_TherapistId",
                        column: x => x.TherapistId,
                        principalTable: "TherapistProfiles",
                        principalColumn: "TherapistId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TherapistDocuments_TherapistId",
                table: "TherapistDocuments",
                column: "TherapistId");

            migrationBuilder.CreateIndex(
                name: "IX_TherapistDocuments_UploadDate",
                table: "TherapistDocuments",
                column: "UploadDate");

            migrationBuilder.CreateIndex(
                name: "IX_TherapistProfiles_TaxId",
                table: "TherapistProfiles",
                column: "TaxId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TherapistDocuments");

            migrationBuilder.DropTable(
                name: "TherapistProfiles");
        }
    }
}
