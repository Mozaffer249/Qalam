using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherEnrollmentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDeadline",
                schema: "course",
                table: "CourseGroupEnrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDeadline",
                schema: "course",
                table: "CourseEnrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "course",
                table: "CourseEnrollmentRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseGroupEnrollments_Status_PaymentDeadline",
                schema: "course",
                table: "CourseGroupEnrollments",
                columns: new[] { "Status", "PaymentDeadline" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_EnrollmentStatus_PaymentDeadline",
                schema: "course",
                table: "CourseEnrollments",
                columns: new[] { "EnrollmentStatus", "PaymentDeadline" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseGroupEnrollments_Status_PaymentDeadline",
                schema: "course",
                table: "CourseGroupEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_EnrollmentStatus_PaymentDeadline",
                schema: "course",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "PaymentDeadline",
                schema: "course",
                table: "CourseGroupEnrollments");

            migrationBuilder.DropColumn(
                name: "PaymentDeadline",
                schema: "course",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "course",
                table: "CourseEnrollmentRequests");
        }
    }
}
