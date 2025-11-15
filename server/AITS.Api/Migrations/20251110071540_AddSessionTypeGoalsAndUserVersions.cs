using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTypeGoalsAndUserVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaseSessionTypeId",
                table: "SessionType",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SessionType",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "SessionType",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Goals",
                table: "SessionType",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "SessionType",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SessionType",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionType_BaseSessionTypeId",
                table: "SessionType",
                column: "BaseSessionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionType_CreatedByUserId",
                table: "SessionType",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionType_IsSystem",
                table: "SessionType",
                column: "IsSystem");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionType_AspNetUsers_CreatedByUserId",
                table: "SessionType",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionType_SessionType_BaseSessionTypeId",
                table: "SessionType",
                column: "BaseSessionTypeId",
                principalTable: "SessionType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Oznacz istniejące typy sesji jako systemowe
            migrationBuilder.Sql(@"
                UPDATE SessionType 
                SET IsSystem = 1, CreatedAt = GETUTCDATE()
                WHERE IsSystem = 0 OR IsSystem IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionType_AspNetUsers_CreatedByUserId",
                table: "SessionType");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionType_SessionType_BaseSessionTypeId",
                table: "SessionType");

            migrationBuilder.DropIndex(
                name: "IX_SessionType_BaseSessionTypeId",
                table: "SessionType");

            migrationBuilder.DropIndex(
                name: "IX_SessionType_CreatedByUserId",
                table: "SessionType");

            migrationBuilder.DropIndex(
                name: "IX_SessionType_IsSystem",
                table: "SessionType");

            migrationBuilder.DropColumn(
                name: "BaseSessionTypeId",
                table: "SessionType");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SessionType");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "SessionType");

            migrationBuilder.DropColumn(
                name: "Goals",
                table: "SessionType");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "SessionType");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SessionType");
        }
    }
}
