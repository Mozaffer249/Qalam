using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                schema: "course",
                table: "CourseEnrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnrollmentRequestId",
                schema: "course",
                table: "CourseEnrollments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PreferredEndDate",
                schema: "course",
                table: "CourseEnrollmentRequests",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "PreferredStartDate",
                schema: "course",
                table: "CourseEnrollmentRequests",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_EnrollmentRequestId",
                schema: "course",
                table: "CourseEnrollments",
                column: "EnrollmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollmentRequests_PreferredStartDate",
                schema: "course",
                table: "CourseEnrollmentRequests",
                column: "PreferredStartDate");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_CourseEnrollmentRequests_EnrollmentRequestId",
                schema: "course",
                table: "CourseEnrollments",
                column: "EnrollmentRequestId",
                principalSchema: "course",
                principalTable: "CourseEnrollmentRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_CourseEnrollmentRequests_EnrollmentRequestId",
                schema: "course",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_EnrollmentRequestId",
                schema: "course",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollmentRequests_PreferredStartDate",
                schema: "course",
                table: "CourseEnrollmentRequests");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                schema: "course",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "EnrollmentRequestId",
                schema: "course",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "PreferredEndDate",
                schema: "course",
                table: "CourseEnrollmentRequests");

            migrationBuilder.DropColumn(
                name: "PreferredStartDate",
                schema: "course",
                table: "CourseEnrollmentRequests");
        }
    }
}
