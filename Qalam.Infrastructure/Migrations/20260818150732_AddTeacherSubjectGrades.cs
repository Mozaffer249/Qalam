using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherSubjectGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeacherSubjectGrades",
                schema: "teacher",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherSubjectId = table.Column<int>(type: "int", nullable: false),
                    GradeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSubjectGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectGrades_Grades_GradeId",
                        column: x => x.GradeId,
                        principalSchema: "education",
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherSubjectGrades_TeacherSubjects_TeacherSubjectId",
                        column: x => x.TeacherSubjectId,
                        principalSchema: "education",
                        principalTable: "TeacherSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectGrades_GradeId",
                schema: "teacher",
                table: "TeacherSubjectGrades",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjectGrades_TeacherSubjectId_GradeId",
                schema: "teacher",
                table: "TeacherSubjectGrades",
                columns: new[] { "TeacherSubjectId", "GradeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeacherSubjectGrades",
                schema: "teacher");
        }
    }
}
