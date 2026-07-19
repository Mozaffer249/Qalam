using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnrollmentSinglePayerAndFixedOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountDue",
                schema: "course",
                table: "Enrollments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PaidByUserId",
                schema: "course",
                table: "Enrollments",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "AllowFlexibleCourses",
                schema: "teaching",
                table: "EducationRules",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.Sql("""
                UPDATE teaching.EducationRules SET AllowFlexibleCourses = 0;
                UPDATE course.Courses SET IsFlexible = 0 WHERE IsFlexible = 1;
                UPDATE e
                SET e.AmountDue = ISNULL(r.EstimatedTotalPrice, 0)
                FROM course.Enrollments e
                LEFT JOIN course.CourseEnrollmentRequests r ON r.Id = e.EnrollmentRequestId
                WHERE e.AmountDue = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_PaidByUserId",
                schema: "course",
                table: "Enrollments",
                column: "PaidByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Users_PaidByUserId",
                schema: "course",
                table: "Enrollments",
                column: "PaidByUserId",
                principalSchema: "security",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Users_PaidByUserId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_PaidByUserId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "AmountDue",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "PaidByUserId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.AlterColumn<bool>(
                name: "AllowFlexibleCourses",
                schema: "teaching",
                table: "EducationRules",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);
        }
    }
}
