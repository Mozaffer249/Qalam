using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherSubjectVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "education",
                table: "TeacherSubjects",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                schema: "education",
                table: "TeacherSubjects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByAdminId",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                schema: "education",
                table: "TeacherSubjects");
        }
    }
}
