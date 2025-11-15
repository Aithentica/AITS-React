using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSessionTypeIdFromSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Usuń foreign key dla SessionTypeId
            migrationBuilder.DropForeignKey(
                name: "FK_Session_SessionType_SessionTypeId",
                table: "Session");

            // Usuń index dla SessionTypeId
            migrationBuilder.DropIndex(
                name: "IX_Session_SessionTypeId",
                table: "Session");

            // Usuń kolumnę SessionTypeId
            migrationBuilder.DropColumn(
                name: "SessionTypeId",
                table: "Session");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Przywróć kolumnę SessionTypeId
            migrationBuilder.AddColumn<int>(
                name: "SessionTypeId",
                table: "Session",
                type: "int",
                nullable: true);

            // Przywróć index
            migrationBuilder.CreateIndex(
                name: "IX_Session_SessionTypeId",
                table: "Session",
                column: "SessionTypeId");

            // Przywróć foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_Session_SessionType_SessionTypeId",
                table: "Session",
                column: "SessionTypeId",
                principalTable: "SessionType",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
