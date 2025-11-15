using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTypeManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionTypeId",
                table: "Session",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SessionType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionTypeQuestion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionTypeId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTypeQuestion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionTypeQuestion_SessionType_SessionTypeId",
                        column: x => x.SessionTypeId,
                        principalTable: "SessionType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionTypeTip",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionTypeId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTypeTip", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionTypeTip_SessionType_SessionTypeId",
                        column: x => x.SessionTypeId,
                        principalTable: "SessionType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Session_SessionTypeId",
                table: "Session",
                column: "SessionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionType_Name",
                table: "SessionType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionTypeQuestion_SessionTypeId_DisplayOrder",
                table: "SessionTypeQuestion",
                columns: new[] { "SessionTypeId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionTypeTip_SessionTypeId_DisplayOrder",
                table: "SessionTypeTip",
                columns: new[] { "SessionTypeId", "DisplayOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_Session_SessionType_SessionTypeId",
                table: "Session",
                column: "SessionTypeId",
                principalTable: "SessionType",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Session_SessionType_SessionTypeId",
                table: "Session");

            migrationBuilder.DropTable(
                name: "SessionTypeQuestion");

            migrationBuilder.DropTable(
                name: "SessionTypeTip");

            migrationBuilder.DropTable(
                name: "SessionType");

            migrationBuilder.DropIndex(
                name: "IX_Session_SessionTypeId",
                table: "Session");

            migrationBuilder.DropColumn(
                name: "SessionTypeId",
                table: "Session");
        }
    }
}
