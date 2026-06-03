using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Course_Session_Units : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseSessionUnits",
                schema: "course",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseSessionId = table.Column<int>(type: "int", nullable: false),
                    ContentUnitId = table.Column<int>(type: "int", nullable: true),
                    LessonId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSessionUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseSessionUnits_ContentUnits_ContentUnitId",
                        column: x => x.ContentUnitId,
                        principalSchema: "education",
                        principalTable: "ContentUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseSessionUnits_CourseSessions_CourseSessionId",
                        column: x => x.CourseSessionId,
                        principalSchema: "course",
                        principalTable: "CourseSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseSessionUnits_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalSchema: "education",
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessionUnits_ContentUnitId",
                schema: "course",
                table: "CourseSessionUnits",
                column: "ContentUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessionUnits_CourseSessionId",
                schema: "course",
                table: "CourseSessionUnits",
                column: "CourseSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessionUnits_LessonId",
                schema: "course",
                table: "CourseSessionUnits",
                column: "LessonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseSessionUnits",
                schema: "course");
        }
    }
}
