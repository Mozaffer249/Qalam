using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherSubjectFieldLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeacherSubjectFieldLevels",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherSubjectId = table.Column<int>(type: "int", nullable: false),
                    WritableFilterValueId = table.Column<int>(type: "int", nullable: false),
                    EducationLevelId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSubjectFieldLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectFieldLevels_EducationLevels_EducationLevelId",
                        column: x => x.EducationLevelId,
                        principalSchema: "education",
                        principalTable: "EducationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectFieldLevels_TeacherSubjects_TeacherSubjectId",
                        column: x => x.TeacherSubjectId,
                        principalSchema: "education",
                        principalTable: "TeacherSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectFieldLevels_WritableFilterValues_WritableFilterValueId",
                        column: x => x.WritableFilterValueId,
                        principalSchema: "education",
                        principalTable: "WritableFilterValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectFieldLevels_EducationLevelId",
                schema: "teacher",
                table: "TeacherSubjectFieldLevels",
                column: "EducationLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectFieldLevels_TeacherSubjectId_WritableFilterValueId_EducationLevelId",
                schema: "teacher",
                table: "TeacherSubjectFieldLevels",
                columns: new[] { "TeacherSubjectId", "WritableFilterValueId", "EducationLevelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectFieldLevels_WritableFilterValueId",
                schema: "teacher",
                table: "TeacherSubjectFieldLevels",
                column: "WritableFilterValueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherSubjectFieldLevels",
                schema: "teacher");
        }
    }
}
