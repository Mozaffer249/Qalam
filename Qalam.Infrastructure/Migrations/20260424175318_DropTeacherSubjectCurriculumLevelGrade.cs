using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropTeacherSubjectCurriculumLevelGrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_Curriculums_CurriculumId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_EducationLevels_LevelId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherSubjects_Grades_GradeId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjects_CurriculumId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjects_GradeId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropIndex(
                name: "IX_TeacherSubjects_LevelId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "CurriculumId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "GradeId",
                schema: "education",
                table: "TeacherSubjects");

            migrationBuilder.DropColumn(
                name: "LevelId",
                schema: "education",
                table: "TeacherSubjects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurriculumId",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GradeId",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LevelId",
                schema: "education",
                table: "TeacherSubjects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_CurriculumId",
                schema: "education",
                table: "TeacherSubjects",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_GradeId",
                schema: "education",
                table: "TeacherSubjects",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_LevelId",
                schema: "education",
                table: "TeacherSubjects",
                column: "LevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_Curriculums_CurriculumId",
                schema: "education",
                table: "TeacherSubjects",
                column: "CurriculumId",
                principalSchema: "education",
                principalTable: "Curriculums",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_EducationLevels_LevelId",
                schema: "education",
                table: "TeacherSubjects",
                column: "LevelId",
                principalSchema: "education",
                principalTable: "EducationLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherSubjects_Grades_GradeId",
                schema: "education",
                table: "TeacherSubjects",
                column: "GradeId",
                principalSchema: "education",
                principalTable: "Grades",
                principalColumn: "Id");
        }
    }
}
