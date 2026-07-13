using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherContentLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeacherContentFolders",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    ParentFolderId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherContentFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherContentFolders_TeacherContentFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalSchema: "teacher",
                        principalTable: "TeacherContentFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherContentFolders_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherContentItems",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    FolderId = table.Column<int>(type: "int", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FileType = table.Column<int>(type: "int", nullable: true),
                    StorageKey = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    PublicUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    TagsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherContentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherContentItems_TeacherContentFolders_FolderId",
                        column: x => x.FolderId,
                        principalSchema: "teacher",
                        principalTable: "TeacherContentFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeacherContentItems_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionContentLinks",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseScheduleId = table.Column<int>(type: "int", nullable: false),
                    ContentItemId = table.Column<int>(type: "int", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionContentLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionContentLinks_CourseSchedules_CourseScheduleId",
                        column: x => x.CourseScheduleId,
                        principalSchema: "course",
                        principalTable: "CourseSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionContentLinks_TeacherContentItems_ContentItemId",
                        column: x => x.ContentItemId,
                        principalSchema: "teacher",
                        principalTable: "TeacherContentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionContentLinks_ContentItemId",
                schema: "teacher",
                table: "SessionContentLinks",
                column: "ContentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionContentLinks_CourseScheduleId_ContentItemId",
                schema: "teacher",
                table: "SessionContentLinks",
                columns: new[] { "CourseScheduleId", "ContentItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherContentFolders_ParentFolderId",
                schema: "teacher",
                table: "TeacherContentFolders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherContentFolders_TeacherId_ParentFolderId_Name",
                schema: "teacher",
                table: "TeacherContentFolders",
                columns: new[] { "TeacherId", "ParentFolderId", "Name" },
                unique: true,
                filter: "[ParentFolderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherContentItems_FolderId",
                schema: "teacher",
                table: "TeacherContentItems",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherContentItems_TeacherId_FolderId",
                schema: "teacher",
                table: "TeacherContentItems",
                columns: new[] { "TeacherId", "FolderId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionContentLinks",
                schema: "teacher");

            migrationBuilder.DropTable(
                name: "TeacherContentItems",
                schema: "teacher");

            migrationBuilder.DropTable(
                name: "TeacherContentFolders",
                schema: "teacher");
        }
    }
}
