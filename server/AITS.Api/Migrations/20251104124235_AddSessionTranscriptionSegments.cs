using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTranscriptionSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionTranscriptionSegment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionTranscriptionId = table.Column<int>(type: "int", nullable: false),
                    SpeakerTag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartOffset = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndOffset = table.Column<TimeSpan>(type: "time", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionTranscriptionSegment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionTranscriptionSegment_SessionTranscription_SessionTranscriptionId",
                        column: x => x.SessionTranscriptionId,
                        principalTable: "SessionTranscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 21,
                column: "Name",
                value: "Transkrypcja ręczna");

            migrationBuilder.UpdateData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 25,
                column: "Name",
                value: "Nagrywanie mikrofonem");

            migrationBuilder.UpdateData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 26,
                column: "Name",
                value: "Microphone recording");

            migrationBuilder.UpdateData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 27,
                column: "Name",
                value: "Przesłany plik audio");

            migrationBuilder.InsertData(
                table: "EnumValues",
                columns: new[] { "Id", "Code", "EnumTypeId" },
                values: new object[,]
                {
                    { 15, "RealtimeRecording", 4 },
                    { 16, "AudioFile", 4 },
                    { 17, "VideoFile", 4 },
                    { 18, "FinalTranscriptUpload", 4 }
                });

            migrationBuilder.InsertData(
                table: "EnumValueTranslations",
                columns: new[] { "Id", "Culture", "EnumValueId", "Name" },
                values: new object[,]
                {
                    { 29, "pl", 15, "Nagrywanie na żywo" },
                    { 30, "en", 15, "Realtime recording" },
                    { 31, "pl", 16, "Transkrypcja pliku audio" },
                    { 32, "en", 16, "Audio file transcription" },
                    { 33, "pl", 17, "Transkrypcja pliku wideo" },
                    { 34, "en", 17, "Video file transcription" },
                    { 35, "pl", 18, "Gotowy transkrypt" },
                    { 36, "en", 18, "Uploaded transcript" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionTranscriptionSegment_SessionTranscriptionId",
                table: "SessionTranscriptionSegment",
                column: "SessionTranscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionTranscriptionSegment");

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.UpdateData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 21,
                column: "Name",
                value: "Tekst ręczny");

            migrationBuilder.UpdateData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 25,
                column: "Name",
                value: "Nagranie audio");

            migrationBuilder.UpdateData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 26,
                column: "Name",
                value: "Audio recording");

            migrationBuilder.UpdateData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 27,
                column: "Name",
                value: "Załadowany plik audio");
        }
    }
}
