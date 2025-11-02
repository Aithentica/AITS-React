using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientsAndSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Patient",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patient_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Session",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    TerapeutaId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GoogleCalendarEventId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoogleMeetLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Session", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Session_AspNetUsers_TerapeutaId",
                        column: x => x.TerapeutaId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Session_Patient_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patient",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    TpayTransactionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payment_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "EnumTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 2, "SessionStatus" },
                    { 3, "PaymentStatus" }
                });

            migrationBuilder.InsertData(
                table: "Translations",
                columns: new[] { "Id", "Culture", "Key", "Value" },
                values: new object[,]
                {
                    { 11, "pl", "dashboard.title", "Kokpit" },
                    { 12, "en", "dashboard.title", "Dashboard" },
                    { 13, "pl", "dashboard.todaySessions", "Dzisiejsze sesje" },
                    { 14, "en", "dashboard.todaySessions", "Today's sessions" },
                    { 15, "pl", "dashboard.allSessions", "Wszystkie sesje" },
                    { 16, "en", "dashboard.allSessions", "All sessions" },
                    { 17, "pl", "dashboard.statistics", "Statystyki" },
                    { 18, "en", "dashboard.statistics", "Statistics" },
                    { 19, "pl", "dashboard.sessionsToday", "Sesje dzisiaj" },
                    { 20, "en", "dashboard.sessionsToday", "Sessions today" },
                    { 21, "pl", "dashboard.sessionsScheduled", "Zaplanowane" },
                    { 22, "en", "dashboard.sessionsScheduled", "Scheduled" },
                    { 23, "pl", "dashboard.sessionsCompleted", "Zakończone w tym miesiącu" },
                    { 24, "en", "dashboard.sessionsCompleted", "Completed this month" },
                    { 25, "pl", "sessions.title", "Sesje" },
                    { 26, "en", "sessions.title", "Sessions" },
                    { 27, "pl", "sessions.new", "Nowa sesja" },
                    { 28, "en", "sessions.new", "New session" },
                    { 29, "pl", "sessions.edit", "Edytuj sesję" },
                    { 30, "en", "sessions.edit", "Edit session" },
                    { 31, "pl", "sessions.details", "Szczegóły sesji" },
                    { 32, "en", "sessions.details", "Session details" },
                    { 33, "pl", "sessions.patient", "Pacjent" },
                    { 34, "en", "sessions.patient", "Patient" },
                    { 35, "pl", "sessions.startTime", "Data rozpoczęcia" },
                    { 36, "en", "sessions.startTime", "Start time" },
                    { 37, "pl", "sessions.endTime", "Data zakończenia" },
                    { 38, "en", "sessions.endTime", "End time" },
                    { 39, "pl", "sessions.price", "Cena" },
                    { 40, "en", "sessions.price", "Price" },
                    { 41, "pl", "sessions.status", "Status" },
                    { 42, "en", "sessions.status", "Status" },
                    { 43, "pl", "sessions.confirm", "Potwierdź" },
                    { 44, "en", "sessions.confirm", "Confirm" },
                    { 45, "pl", "sessions.cancel", "Anuluj" },
                    { 46, "en", "sessions.cancel", "Cancel" },
                    { 47, "pl", "sessions.sendNotification", "Wyślij powiadomienie" },
                    { 48, "en", "sessions.sendNotification", "Send notification" },
                    { 49, "pl", "sessions.googleMeet", "Link Google Meet" },
                    { 50, "en", "sessions.googleMeet", "Google Meet link" },
                    { 51, "pl", "sessions.notes", "Notatki" },
                    { 52, "en", "sessions.notes", "Notes" },
                    { 53, "pl", "sessions.save", "Zapisz" },
                    { 54, "en", "sessions.save", "Save" },
                    { 55, "pl", "sessions.delete", "Usuń" },
                    { 56, "en", "sessions.delete", "Delete" },
                    { 57, "pl", "patients.title", "Pacjenci" },
                    { 58, "en", "patients.title", "Patients" },
                    { 59, "pl", "patients.new", "Nowy pacjent" },
                    { 60, "en", "patients.new", "New patient" },
                    { 61, "pl", "patients.edit", "Edytuj pacjenta" },
                    { 62, "en", "patients.edit", "Edit patient" },
                    { 63, "pl", "patients.firstName", "Imię" },
                    { 64, "en", "patients.firstName", "First name" },
                    { 65, "pl", "patients.lastName", "Nazwisko" },
                    { 66, "en", "patients.lastName", "Last name" },
                    { 67, "pl", "patients.email", "E-mail" },
                    { 68, "en", "patients.email", "Email" },
                    { 69, "pl", "patients.phone", "Telefon" },
                    { 70, "en", "patients.phone", "Phone" },
                    { 71, "pl", "patients.notes", "Notatki" },
                    { 72, "en", "patients.notes", "Notes" },
                    { 73, "pl", "payments.title", "Płatności" },
                    { 74, "en", "payments.title", "Payments" },
                    { 75, "pl", "payments.create", "Utwórz płatność" },
                    { 76, "en", "payments.create", "Create payment" },
                    { 77, "pl", "payments.amount", "Kwota" },
                    { 78, "en", "payments.amount", "Amount" },
                    { 79, "pl", "payments.pay", "Zapłać" },
                    { 80, "en", "payments.pay", "Pay" },
                    { 81, "pl", "sms.session.confirmed", "Sesja potwierdzona na {date} o {time}. Link do spotkania: {link}" },
                    { 82, "en", "sms.session.confirmed", "Session confirmed on {date} at {time}. Meeting link: {link}" },
                    { 83, "pl", "sms.session.changed", "Sesja została zmieniona. Nowa data: {date} o {time}. Link: {link}" },
                    { 84, "en", "sms.session.changed", "Session has been changed. New date: {date} at {time}. Link: {link}" },
                    { 85, "pl", "sms.session.cancelled", "Sesja na {date} została anulowana." },
                    { 86, "en", "sms.session.cancelled", "Session on {date} has been cancelled." }
                });

            migrationBuilder.InsertData(
                table: "EnumValues",
                columns: new[] { "Id", "Code", "EnumTypeId" },
                values: new object[,]
                {
                    { 4, "Scheduled", 2 },
                    { 5, "Confirmed", 2 },
                    { 6, "Completed", 2 },
                    { 7, "Cancelled", 2 },
                    { 8, "Pending", 3 },
                    { 9, "Completed", 3 },
                    { 10, "Failed", 3 }
                });

            migrationBuilder.InsertData(
                table: "EnumValueTranslations",
                columns: new[] { "Id", "Culture", "EnumValueId", "Name" },
                values: new object[,]
                {
                    { 7, "pl", 4, "Zaplanowana" },
                    { 8, "en", 4, "Scheduled" },
                    { 9, "pl", 5, "Potwierdzona" },
                    { 10, "en", 5, "Confirmed" },
                    { 11, "pl", 6, "Zakończona" },
                    { 12, "en", 6, "Completed" },
                    { 13, "pl", 7, "Anulowana" },
                    { 14, "en", 7, "Cancelled" },
                    { 15, "pl", 8, "Oczekująca" },
                    { 16, "en", 8, "Pending" },
                    { 17, "pl", 9, "Zrealizowana" },
                    { 18, "en", 9, "Completed" },
                    { 19, "pl", 10, "Nieudana" },
                    { 20, "en", 10, "Failed" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patient_CreatedByUserId",
                table: "Patient",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Patient_Email",
                table: "Patient",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_SessionId",
                table: "Payment",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_TpayTransactionId",
                table: "Payment",
                column: "TpayTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Session_PatientId",
                table: "Session",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Session_StartDateTime",
                table: "Session",
                column: "StartDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Session_TerapeutaId",
                table: "Session",
                column: "TerapeutaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "Session");

            migrationBuilder.DropTable(
                name: "Patient");

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "Translations",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "EnumTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "EnumTypes",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
