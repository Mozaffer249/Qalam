using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SessionAttendanceAndLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                schema: "course",
                table: "Enrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserId",
                schema: "course",
                table: "Enrollments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                schema: "course",
                table: "CourseSchedules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                schema: "course",
                table: "CourseSchedules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeacherNote",
                schema: "course",
                table: "CourseSchedules",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SessionAttendances",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseScheduleId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsAutoResolved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionAttendances_CourseSchedules_CourseScheduleId",
                        column: x => x.CourseScheduleId,
                        principalSchema: "course",
                        principalTable: "CourseSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionAttendances_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CancelledByUserId",
                schema: "course",
                table: "Enrollments",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAttendances_CourseScheduleId_StudentId",
                schema: "course",
                table: "SessionAttendances",
                columns: new[] { "CourseScheduleId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionAttendances_Status",
                schema: "course",
                table: "SessionAttendances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAttendances_StudentId",
                schema: "course",
                table: "SessionAttendances",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Users_CancelledByUserId",
                schema: "course",
                table: "Enrollments",
                column: "CancelledByUserId",
                principalSchema: "security",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Users_CancelledByUserId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropTable(
                name: "SessionAttendances",
                schema: "course");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_CancelledByUserId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropColumn(
                name: "TeacherNote",
                schema: "course",
                table: "CourseSchedules");
        }
    }
}
