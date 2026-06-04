using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentRequestSessionUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseRequestProposedSessionUnits",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseRequestProposedSessionId = table.Column<int>(type: "int", nullable: false),
                    ContentUnitId = table.Column<int>(type: "int", nullable: true),
                    LessonId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseRequestProposedSessionUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseRequestProposedSessionUnits_ContentUnits_ContentUnitId",
                        column: x => x.ContentUnitId,
                        principalSchema: "education",
                        principalTable: "ContentUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseRequestProposedSessionUnits_CourseRequestProposedSessions_CourseRequestProposedSessionId",
                        column: x => x.CourseRequestProposedSessionId,
                        principalSchema: "course",
                        principalTable: "CourseRequestProposedSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseRequestProposedSessionUnits_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalSchema: "education",
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseRequestSelectedSessionSlotUnits",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseRequestSelectedSessionSlotId = table.Column<int>(type: "int", nullable: false),
                    ContentUnitId = table.Column<int>(type: "int", nullable: true),
                    LessonId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseRequestSelectedSessionSlotUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseRequestSelectedSessionSlotUnits_ContentUnits_ContentUnitId",
                        column: x => x.ContentUnitId,
                        principalSchema: "education",
                        principalTable: "ContentUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseRequestSelectedSessionSlotUnits_CourseRequestSelectedSessionSlots_CourseRequestSelectedSessionSlotId",
                        column: x => x.CourseRequestSelectedSessionSlotId,
                        principalSchema: "course",
                        principalTable: "CourseRequestSelectedSessionSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseRequestSelectedSessionSlotUnits_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalSchema: "education",
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestProposedSessionUnits_ContentUnitId",
                schema: "course",
                table: "CourseRequestProposedSessionUnits",
                column: "ContentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestProposedSessionUnits_CourseRequestProposedSessionId",
                schema: "course",
                table: "CourseRequestProposedSessionUnits",
                column: "CourseRequestProposedSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestProposedSessionUnits_LessonId",
                schema: "course",
                table: "CourseRequestProposedSessionUnits",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestSelectedSessionSlotUnits_ContentUnitId",
                schema: "course",
                table: "CourseRequestSelectedSessionSlotUnits",
                column: "ContentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestSelectedSessionSlotUnits_CourseRequestSelectedSessionSlotId",
                schema: "course",
                table: "CourseRequestSelectedSessionSlotUnits",
                column: "CourseRequestSelectedSessionSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRequestSelectedSessionSlotUnits_LessonId",
                schema: "course",
                table: "CourseRequestSelectedSessionSlotUnits",
                column: "LessonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseRequestProposedSessionUnits",
                schema: "course");

            migrationBuilder.DropTable(
                name: "CourseRequestSelectedSessionSlotUnits",
                schema: "course");
        }
    }
}
