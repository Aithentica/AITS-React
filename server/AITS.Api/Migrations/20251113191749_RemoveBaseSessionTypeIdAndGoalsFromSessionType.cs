using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBaseSessionTypeIdAndGoalsFromSessionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Usuń foreign key i index dla BaseSessionTypeId
            migrationBuilder.DropForeignKey(
                name: "FK_SessionType_SessionType_BaseSessionTypeId",
                table: "SessionType");

            migrationBuilder.DropIndex(
                name: "IX_SessionType_BaseSessionTypeId",
                table: "SessionType");

            // Usuń kolumny
            migrationBuilder.DropColumn(
                name: "BaseSessionTypeId",
                table: "SessionType");

            migrationBuilder.DropColumn(
                name: "Goals",
                table: "SessionType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Przywróć kolumny
            migrationBuilder.AddColumn<int>(
                name: "BaseSessionTypeId",
                table: "SessionType",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Goals",
                table: "SessionType",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            // Przywróć index i foreign key
            migrationBuilder.CreateIndex(
                name: "IX_SessionType_BaseSessionTypeId",
                table: "SessionType",
                column: "BaseSessionTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionType_SessionType_BaseSessionTypeId",
                table: "SessionType",
                column: "BaseSessionTypeId",
                principalTable: "SessionType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
