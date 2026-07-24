using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SessionJoinAndReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeacherReviews_Students_StudentId",
                table: "TeacherReviews");

            migrationBuilder.DropIndex(
                name: "IX_TeacherReviews_StudentId",
                table: "TeacherReviews");

            migrationBuilder.AddColumn<DateTime>(
                name: "JoinedAt",
                schema: "course",
                table: "SessionAttendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeacherAttendanceStatus",
                schema: "course",
                table: "CourseSchedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TeacherJoinedAt",
                schema: "course",
                table: "CourseSchedules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherReviews_SessionId",
                table: "TeacherReviews",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherReviews_StudentId_SessionId",
                table: "TeacherReviews",
                columns: new[] { "StudentId", "SessionId" },
                unique: true,
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherReviews_Students_StudentId",
                table: "TeacherReviews",
                column: "StudentId",
                principalSchema: "student",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeacherReviews_Students_StudentId",
                table: "TeacherReviews");

            migrationBuilder.DropIndex(
                name: "IX_TeacherReviews_SessionId",
                table: "TeacherReviews");

            migrationBuilder.DropIndex(
                name: "IX_TeacherReviews_StudentId_SessionId",
                table: "TeacherReviews");

            migrationBuilder.DropColumn(
                name: "JoinedAt",
                schema: "course",
                table: "SessionAttendances");

            migrationBuilder.DropColumn(
                name: "TeacherAttendanceStatus",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropColumn(
                name: "TeacherJoinedAt",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherReviews_StudentId",
                table: "TeacherReviews",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherReviews_Students_StudentId",
                table: "TeacherReviews",
                column: "StudentId",
                principalSchema: "student",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
