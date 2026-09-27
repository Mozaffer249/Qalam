using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GlobalTeacherInterview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InterviewUnlockCourseScheduleId",
                table: "Teachers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InterviewUnlockEnrollmentId",
                table: "Teachers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InterviewUnlockSource",
                table: "Teachers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "InterviewUnlockedAt",
                table: "Teachers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_InterviewUnlockCourseScheduleId",
                table: "Teachers",
                column: "InterviewUnlockCourseScheduleId");

            // Account-wide interview: earliest per-domain unlock becomes the teacher's single unlock.
            migrationBuilder.Sql(@"
UPDATE t
SET t.HasCompletedInterviewSession = 1,
    t.InterviewUnlockSource = p.InterviewUnlockSource,
    t.InterviewUnlockEnrollmentId = p.InterviewUnlockEnrollmentId,
    t.InterviewUnlockCourseScheduleId = p.InterviewUnlockCourseScheduleId,
    t.InterviewUnlockedAt = p.InterviewUnlockedAt
FROM [Teachers] t
CROSS APPLY (
    SELECT TOP 1 dp.InterviewUnlockSource, dp.InterviewUnlockEnrollmentId,
                 dp.InterviewUnlockCourseScheduleId, dp.InterviewUnlockedAt
    FROM [teacher].[TeacherDomainPricings] dp
    WHERE dp.TeacherId = t.Id AND dp.HasCompletedInterviewSession = 1
    ORDER BY CASE WHEN dp.InterviewUnlockedAt IS NULL THEN 1 ELSE 0 END, dp.InterviewUnlockedAt, dp.Id
) p;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teachers_InterviewUnlockCourseScheduleId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "InterviewUnlockCourseScheduleId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "InterviewUnlockEnrollmentId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "InterviewUnlockSource",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "InterviewUnlockedAt",
                table: "Teachers");
        }
    }
}
