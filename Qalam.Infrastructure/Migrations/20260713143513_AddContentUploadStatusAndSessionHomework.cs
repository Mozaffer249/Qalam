using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContentUploadStatusAndSessionHomework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UploadStatus",
                schema: "teacher",
                table: "TeacherContentItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SessionHomeworkAssignments",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseScheduleId = table.Column<int>(type: "int", nullable: false),
                    ContentItemId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionHomeworkAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionHomeworkAssignments_CourseSchedules_CourseScheduleId",
                        column: x => x.CourseScheduleId,
                        principalSchema: "course",
                        principalTable: "CourseSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionHomeworkAssignments_TeacherContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalSchema: "teacher",
                        principalTable: "TeacherContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionHomeworkAssignments_ContentItemId",
                schema: "teacher",
                table: "SessionHomeworkAssignments",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionHomeworkAssignments_CourseScheduleId",
                schema: "teacher",
                table: "SessionHomeworkAssignments",
                column: "CourseScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionHomeworkAssignments",
                schema: "teacher");

            migrationBuilder.DropColumn(
                name: "UploadStatus",
                schema: "teacher",
                table: "TeacherContentItems");
        }
    }
}
