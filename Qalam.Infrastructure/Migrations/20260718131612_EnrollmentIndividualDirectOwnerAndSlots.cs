using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnrollmentIndividualDirectOwnerAndSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                schema: "course",
                table: "Enrollments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PreferredEndDate",
                schema: "course",
                table: "Enrollments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PreferredStartDate",
                schema: "course",
                table: "Enrollments",
                type: "date",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE e
                SET
                    OwnerUserId = r.RequestedByUserId,
                    PreferredStartDate = r.PreferredStartDate,
                    PreferredEndDate = r.PreferredEndDate
                FROM course.Enrollments e
                INNER JOIN course.CourseEnrollmentRequests r ON r.Id = e.EnrollmentRequestId
                WHERE e.EnrollmentRequestId IS NOT NULL;
                """);

            migrationBuilder.CreateTable(
                name: "EnrollmentSelectedSessionSlots",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    SessionNumber = table.Column<int>(type: "int", nullable: false),
                    TeacherAvailabilityId = table.Column<int>(type: "int", nullable: false),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentSelectedSessionSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnrollmentSelectedSessionSlots_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalSchema: "course",
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnrollmentSelectedSessionSlots_TeacherAvailabilities_TeacherAvailabilityId",
                        column: x => x.TeacherAvailabilityId,
                        principalTable: "TeacherAvailabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentSelectedSessionSlotUnits",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentSelectedSessionSlotId = table.Column<int>(type: "int", nullable: false),
                    ContentUnitId = table.Column<int>(type: "int", nullable: true),
                    LessonId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentSelectedSessionSlotUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnrollmentSelectedSessionSlotUnits_ContentUnits_ContentUnitId",
                        column: x => x.ContentUnitId,
                        principalSchema: "education",
                        principalTable: "ContentUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnrollmentSelectedSessionSlotUnits_EnrollmentSelectedSessionSlots_EnrollmentSelectedSessionSlotId",
                        column: x => x.EnrollmentSelectedSessionSlotId,
                        principalSchema: "course",
                        principalTable: "EnrollmentSelectedSessionSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnrollmentSelectedSessionSlotUnits_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalSchema: "education",
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_OwnerUserId",
                schema: "course",
                table: "Enrollments",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentSelectedSessionSlots_EnrollmentId",
                schema: "course",
                table: "EnrollmentSelectedSessionSlots",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentSelectedSessionSlots_EnrollmentId_SessionNumber",
                schema: "course",
                table: "EnrollmentSelectedSessionSlots",
                columns: new[] { "EnrollmentId", "SessionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentSelectedSessionSlots_TeacherAvailabilityId",
                schema: "course",
                table: "EnrollmentSelectedSessionSlots",
                column: "TeacherAvailabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentSelectedSessionSlotUnits_ContentUnitId",
                schema: "course",
                table: "EnrollmentSelectedSessionSlotUnits",
                column: "ContentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentSelectedSessionSlotUnits_EnrollmentSelectedSessionSlotId",
                schema: "course",
                table: "EnrollmentSelectedSessionSlotUnits",
                column: "EnrollmentSelectedSessionSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentSelectedSessionSlotUnits_LessonId",
                schema: "course",
                table: "EnrollmentSelectedSessionSlotUnits",
                column: "LessonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Users_OwnerUserId",
                schema: "course",
                table: "Enrollments",
                column: "OwnerUserId",
                principalSchema: "security",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Users_OwnerUserId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropTable(
                name: "EnrollmentSelectedSessionSlotUnits",
                schema: "course");

            migrationBuilder.DropTable(
                name: "EnrollmentSelectedSessionSlots",
                schema: "course");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_OwnerUserId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "PreferredEndDate",
                schema: "course",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "PreferredStartDate",
                schema: "course",
                table: "Enrollments");
        }
    }
}
