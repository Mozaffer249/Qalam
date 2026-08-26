using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFreeTrialConsumptionLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InterviewRevertedAt",
                schema: "teacher",
                table: "TeacherDomainPricings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InterviewUnlockCourseScheduleId",
                schema: "teacher",
                table: "TeacherDomainPricings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InterviewUnlockEnrollmentId",
                schema: "teacher",
                table: "TeacherDomainPricings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InterviewUnlockSource",
                schema: "teacher",
                table: "TeacherDomainPricings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "InterviewUnlockedAt",
                schema: "teacher",
                table: "TeacherDomainPricings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentFreeTrialConsumptions",
                schema: "student",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    OpenSessionRequestId = table.Column<int>(type: "int", nullable: true),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    DomainId = table.Column<int>(type: "int", nullable: false),
                    CourseScheduleId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RestoredEligibility = table.Column<bool>(type: "bit", nullable: false),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFreeTrialConsumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentFreeTrialConsumptions_CourseSchedules_CourseScheduleId",
                        column: x => x.CourseScheduleId,
                        principalSchema: "course",
                        principalTable: "CourseSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentFreeTrialConsumptions_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalSchema: "course",
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentFreeTrialConsumptions_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDomainPricings_InterviewUnlockEnrollmentId",
                schema: "teacher",
                table: "TeacherDomainPricings",
                column: "InterviewUnlockEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFreeTrialConsumptions_CourseScheduleId",
                schema: "student",
                table: "StudentFreeTrialConsumptions",
                column: "CourseScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFreeTrialConsumptions_EnrollmentId",
                schema: "student",
                table: "StudentFreeTrialConsumptions",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFreeTrialConsumptions_Status",
                schema: "student",
                table: "StudentFreeTrialConsumptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFreeTrialConsumptions_StudentId",
                schema: "student",
                table: "StudentFreeTrialConsumptions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFreeTrialConsumptions_StudentId_Status",
                schema: "student",
                table: "StudentFreeTrialConsumptions",
                columns: new[] { "StudentId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentFreeTrialConsumptions",
                schema: "student");

            migrationBuilder.DropIndex(
                name: "IX_TeacherDomainPricings_InterviewUnlockEnrollmentId",
                schema: "teacher",
                table: "TeacherDomainPricings");

            migrationBuilder.DropColumn(
                name: "InterviewRevertedAt",
                schema: "teacher",
                table: "TeacherDomainPricings");

            migrationBuilder.DropColumn(
                name: "InterviewUnlockCourseScheduleId",
                schema: "teacher",
                table: "TeacherDomainPricings");

            migrationBuilder.DropColumn(
                name: "InterviewUnlockEnrollmentId",
                schema: "teacher",
                table: "TeacherDomainPricings");

            migrationBuilder.DropColumn(
                name: "InterviewUnlockSource",
                schema: "teacher",
                table: "TeacherDomainPricings");

            migrationBuilder.DropColumn(
                name: "InterviewUnlockedAt",
                schema: "teacher",
                table: "TeacherDomainPricings");
        }
    }
}
