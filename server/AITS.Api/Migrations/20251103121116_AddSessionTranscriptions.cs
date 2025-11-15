using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTranscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionTranscription",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    TranscriptText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    SourceFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTranscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionTranscription_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SessionTranscription_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "EnumTypes",
                columns: new[] { "Id", "Name" },
                values: new object[] { 4, "SessionTranscriptionSource" });

            migrationBuilder.InsertData(
                table: "EnumValues",
                columns: new[] { "Id", "Code", "EnumTypeId" },
                values: new object[,]
                {
                    { 11, "ManualText", 4 },
                    { 12, "TextFile", 4 },
                    { 13, "AudioRecording", 4 },
                    { 14, "AudioUpload", 4 }
                });

            migrationBuilder.InsertData(
                table: "EnumValueTranslations",
                columns: new[] { "Id", "Culture", "EnumValueId", "Name" },
                values: new object[,]
                {
                    { 21, "pl", 11, "Tekst ręczny" },
                    { 22, "en", 11, "Manual text" },
                    { 23, "pl", 12, "Plik tekstowy" },
                    { 24, "en", 12, "Text file" },
                    { 25, "pl", 13, "Nagranie audio" },
                    { 26, "en", 13, "Audio recording" },
                    { 27, "pl", 14, "Załadowany plik audio" },
                    { 28, "en", 14, "Uploaded audio file" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionTranscription_CreatedByUserId",
                table: "SessionTranscription",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionTranscription_SessionId",
                table: "SessionTranscription",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionTranscription");

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "EnumTypes",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
