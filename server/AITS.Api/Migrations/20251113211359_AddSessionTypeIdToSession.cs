using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTypeIdToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionTypeId",
                table: "Session",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Session_SessionTypeId",
                table: "Session",
                column: "SessionTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Session_SessionType_SessionTypeId",
                table: "Session",
                column: "SessionTypeId",
                principalTable: "SessionType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Session_SessionType_SessionTypeId",
                table: "Session");

            migrationBuilder.DropIndex(
                name: "IX_Session_SessionTypeId",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "SessionTypeId",
                table: "Session");
        }
    }
}
