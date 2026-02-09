using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCourseEntityWithTitleDescriptionAndDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "course",
                table: "Courses",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DomainId",
                schema: "course",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                schema: "course",
                table: "Courses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "course",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                schema: "course",
                table: "Courses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "course",
                table: "Courses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DomainId",
                schema: "course",
                table: "Courses",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_StartDate_EndDate",
                schema: "course",
                table: "Courses",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Status_IsActive",
                schema: "course",
                table: "Courses",
                columns: new[] { "Status", "IsActive" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Course_SessionDuration",
                schema: "course",
                table: "Courses",
                sql: "([IsFlexible] = 1) OR ([SessionDurationMinutes] IS NOT NULL AND [SessionDurationMinutes] > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Course_SessionsCount",
                schema: "course",
                table: "Courses",
                sql: "([IsFlexible] = 1) OR ([SessionsCount] IS NOT NULL AND [SessionsCount] > 0)");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_EducationDomains_DomainId",
                schema: "course",
                table: "Courses",
                column: "DomainId",
                principalSchema: "education",
                principalTable: "EducationDomains",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_EducationDomains_DomainId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_DomainId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_StartDate_EndDate",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_Status_IsActive",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Course_SessionDuration",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Course_SessionsCount",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DomainId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "course",
                table: "Courses");
        }
    }
}
