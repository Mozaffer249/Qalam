using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionHomeworkFileLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionHomeworkAssignments_TeacherContentItems_ContentItemId",
                schema: "teacher",
                table: "SessionHomeworkAssignments");

            migrationBuilder.DropIndex(
                name: "IX_SessionHomeworkAssignments_ContentItemId",
                schema: "teacher",
                table: "SessionHomeworkAssignments");

            migrationBuilder.DropColumn(
                name: "ContentItemId",
                schema: "teacher",
                table: "SessionHomeworkAssignments");

            migrationBuilder.CreateTable(
                name: "SessionHomeworkFileLinks",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionHomeworkAssignmentId = table.Column<int>(type: "int", nullable: false),
                    ContentItemId = table.Column<int>(type: "int", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionHomeworkFileLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionHomeworkFileLinks_SessionHomeworkAssignments_SessionHomeworkAssignmentId",
                        column: x => x.SessionHomeworkAssignmentId,
                        principalSchema: "teacher",
                        principalTable: "SessionHomeworkAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionHomeworkFileLinks_TeacherContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalSchema: "teacher",
                        principalTable: "TeacherContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionHomeworkFileLinks_ContentItemId",
                schema: "teacher",
                table: "SessionHomeworkFileLinks",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionHomeworkFileLinks_SessionHomeworkAssignmentId_ContentItemId",
                schema: "teacher",
                table: "SessionHomeworkFileLinks",
                columns: new[] { "SessionHomeworkAssignmentId", "ContentItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionHomeworkFileLinks",
                schema: "teacher");

            migrationBuilder.AddColumn<int>(
                name: "ContentItemId",
                schema: "teacher",
                table: "SessionHomeworkAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionHomeworkAssignments_ContentItemId",
                schema: "teacher",
                table: "SessionHomeworkAssignments",
                column: "ContentItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionHomeworkAssignments_TeacherContentItems_ContentItemId",
                schema: "teacher",
                table: "SessionHomeworkAssignments",
                column: "ContentItemId",
                principalSchema: "teacher",
                principalTable: "TeacherContentItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
