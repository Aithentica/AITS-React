using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AITS.Api.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUserRolesAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Patient",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "TherapistProfileTherapistId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserRoleMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    AssignedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleMapping_AspNetUsers_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserRoleMapping_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "EnumTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 5, "UserRole" },
                    { 6, "UserStatus" }
                });

            migrationBuilder.InsertData(
                table: "EnumValues",
                columns: new[] { "Id", "Code", "EnumTypeId" },
                values: new object[,]
                {
                    { 19, "Administrator", 5 },
                    { 20, "Terapeuta", 5 },
                    { 21, "TerapeutaFreeAccess", 5 },
                    { 22, "Pacjent", 5 },
                    { 23, "Active", 6 },
                    { 24, "Inactive", 6 },
                    { 25, "PendingVerification", 6 },
                    { 26, "Suspended", 6 }
                });

            migrationBuilder.InsertData(
                table: "EnumValueTranslations",
                columns: new[] { "Id", "Culture", "EnumValueId", "Name" },
                values: new object[,]
                {
                    { 37, "pl", 19, "Administrator" },
                    { 38, "en", 19, "Administrator" },
                    { 39, "pl", 20, "Terapeuta" },
                    { 40, "en", 20, "Therapist" },
                    { 41, "pl", 21, "Terapeuta z darmowym dostępem" },
                    { 42, "en", 21, "Therapist with free access" },
                    { 43, "pl", 22, "Pacjent" },
                    { 44, "en", 22, "Patient" },
                    { 45, "pl", 23, "Aktywny" },
                    { 46, "en", 23, "Active" },
                    { 47, "pl", 24, "Nieaktywny" },
                    { 48, "en", 24, "Inactive" },
                    { 49, "pl", 25, "Oczekuje na weryfikację" },
                    { 50, "en", 25, "Pending verification" },
                    { 51, "pl", 26, "Zawieszony" },
                    { 52, "en", 26, "Suspended" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patient_UserId",
                table: "Patient",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_StatusId",
                table: "AspNetUsers",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TherapistProfileTherapistId",
                table: "AspNetUsers",
                column: "TherapistProfileTherapistId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleMapping_AssignedByUserId",
                table: "UserRoleMapping",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleMapping_IsActive",
                table: "UserRoleMapping",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleMapping_RoleId",
                table: "UserRoleMapping",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleMapping_UserId",
                table: "UserRoleMapping",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleMapping_UserId_RoleId",
                table: "UserRoleMapping",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_TherapistProfiles_TherapistProfileTherapistId",
                table: "AspNetUsers",
                column: "TherapistProfileTherapistId",
                principalTable: "TherapistProfiles",
                principalColumn: "TherapistId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patient_AspNetUsers_UserId",
                table: "Patient",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Czyszczenie potencjalnych konfliktów przed migracją ról
            // Usuń wszystkie istniejące mapowania jeśli istnieją (na wypadek ponownej migracji)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRoleMapping')
                BEGIN
                    DELETE FROM UserRoleMapping;
                END
            ");

            // Migracja istniejących ról z AspNetUserRoles do UserRoleMapping
            migrationBuilder.Sql(@"
                INSERT INTO UserRoleMapping (UserId, RoleId, AssignedAt, IsActive)
                SELECT 
                    ur.UserId,
                    CASE r.Name
                        WHEN 'Administrator' THEN 1
                        WHEN 'Terapeuta' THEN 2
                        WHEN 'TerapeutaFreeAccess' THEN 3
                        WHEN 'Pacjent' THEN 4
                        ELSE NULL
                    END AS RoleId,
                    GETUTCDATE() AS AssignedAt,
                    1 AS IsActive
                FROM AspNetUserRoles ur
                INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                WHERE CASE r.Name
                    WHEN 'Administrator' THEN 1
                    WHEN 'Terapeuta' THEN 2
                    WHEN 'TerapeutaFreeAccess' THEN 3
                    WHEN 'Pacjent' THEN 4
                    ELSE NULL
                END IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM UserRoleMapping urm 
                    WHERE urm.UserId = ur.UserId 
                    AND urm.RoleId = CASE r.Name
                        WHEN 'Administrator' THEN 1
                        WHEN 'Terapeuta' THEN 2
                        WHEN 'TerapeutaFreeAccess' THEN 3
                        WHEN 'Pacjent' THEN 4
                        ELSE NULL
                    END
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_TherapistProfiles_TherapistProfileTherapistId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Patient_AspNetUsers_UserId",
                table: "Patient");

            migrationBuilder.DropTable(
                name: "UserRoleMapping");

            migrationBuilder.DropIndex(
                name: "IX_Patient_UserId",
                table: "Patient");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_StatusId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TherapistProfileTherapistId",
                table: "AspNetUsers");

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "EnumValueTranslations",
                keyColumn: "Id",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "EnumValues",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "EnumTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "EnumTypes",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TherapistProfileTherapistId",
                table: "AspNetUsers");
        }
    }
}
