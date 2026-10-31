using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentIdentityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentityNumber",
                schema: "student",
                table: "Students",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdentityType",
                schema: "student",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuingCountryCode",
                schema: "student",
                table: "Students",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NationalityId",
                schema: "student",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_IdentityType_IdentityNumber",
                schema: "student",
                table: "Students",
                columns: new[] { "IdentityType", "IdentityNumber" },
                unique: true,
                filter: "[IdentityNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Students_NationalityId",
                schema: "student",
                table: "Students",
                column: "NationalityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Nationalities_NationalityId",
                schema: "student",
                table: "Students",
                column: "NationalityId",
                principalSchema: "common",
                principalTable: "Nationalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Nationalities_NationalityId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_IdentityType_IdentityNumber",
                schema: "student",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_NationalityId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "IdentityNumber",
                schema: "student",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "IdentityType",
                schema: "student",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "IssuingCountryCode",
                schema: "student",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "NationalityId",
                schema: "student",
                table: "Students");
        }
    }
}
