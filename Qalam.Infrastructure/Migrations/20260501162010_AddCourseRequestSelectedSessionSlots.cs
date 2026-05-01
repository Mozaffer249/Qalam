using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseRequestSelectedSessionSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseRequestSelectedSessionSlots",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseEnrollmentRequestId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CourseRequestSelectedSessionSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseRequestSelectedSessionSlots_CourseEnrollmentRequests_CourseEnrollmentRequestId",
                        column: x => x.CourseEnrollmentRequestId,
                        principalSchema: "course",
                        principalTable: "CourseEnrollmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseRequestSelectedSessionSlots_TeacherAvailabilities_TeacherAvailabilityId",
                        column: x => x.TeacherAvailabilityId,
                        principalTable: "TeacherAvailabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestSelectedSessionSlots_CourseEnrollmentRequestId",
                schema: "course",
                table: "CourseRequestSelectedSessionSlots",
                column: "CourseEnrollmentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestSelectedSessionSlots_CourseEnrollmentRequestId_SessionNumber",
                schema: "course",
                table: "CourseRequestSelectedSessionSlots",
                columns: new[] { "CourseEnrollmentRequestId", "SessionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestSelectedSessionSlots_TeacherAvailabilityId",
                schema: "course",
                table: "CourseRequestSelectedSessionSlots",
                column: "TeacherAvailabilityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseRequestSelectedSessionSlots",
                schema: "course");
        }
    }
}
