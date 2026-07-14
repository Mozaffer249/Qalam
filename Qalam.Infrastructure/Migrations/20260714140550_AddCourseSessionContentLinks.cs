using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseSessionContentLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseSessionId",
                schema: "course",
                table: "CourseSchedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CourseSessionContentLinks",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseSessionId = table.Column<int>(type: "int", nullable: false),
                    ContentItemId = table.Column<int>(type: "int", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSessionContentLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseSessionContentLinks_CourseSessions_CourseSessionId",
                        column: x => x.CourseSessionId,
                        principalSchema: "course",
                        principalTable: "CourseSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseSessionContentLinks_TeacherContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalSchema: "teacher",
                        principalTable: "TeacherContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSchedules_CourseSessionId",
                schema: "course",
                table: "CourseSchedules",
                column: "CourseSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessionContentLinks_ContentItemId",
                schema: "teacher",
                table: "CourseSessionContentLinks",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessionContentLinks_CourseSessionId_ContentItemId",
                schema: "teacher",
                table: "CourseSessionContentLinks",
                columns: new[] { "CourseSessionId", "ContentItemId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseSchedules_CourseSessions_CourseSessionId",
                schema: "course",
                table: "CourseSchedules",
                column: "CourseSessionId",
                principalSchema: "course",
                principalTable: "CourseSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseSchedules_CourseSessions_CourseSessionId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropTable(
                name: "CourseSessionContentLinks",
                schema: "teacher");

            migrationBuilder.DropIndex(
                name: "IX_CourseSchedules_CourseSessionId",
                schema: "course",
                table: "CourseSchedules");

            migrationBuilder.DropColumn(
                name: "CourseSessionId",
                schema: "course",
                table: "CourseSchedules");
        }
    }
}
