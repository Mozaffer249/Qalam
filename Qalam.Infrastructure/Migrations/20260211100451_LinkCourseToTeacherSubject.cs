using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkCourseToTeacherSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Curriculums_CurriculumId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_EducationDomains_DomainId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_EducationLevels_LevelId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Grades_GradeId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Subjects_SubjectId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_CurriculumId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_DomainId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_GradeId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_LevelId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CurriculumId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DomainId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "GradeId",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "LevelId",
                schema: "course",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "SubjectId",
                schema: "course",
                table: "Courses",
                newName: "TeacherSubjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Courses_SubjectId",
                schema: "course",
                table: "Courses",
                newName: "IX_Courses_TeacherSubjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_TeacherSubjects_TeacherSubjectId",
                schema: "course",
                table: "Courses",
                column: "TeacherSubjectId",
                principalSchema: "education",
                principalTable: "TeacherSubjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_TeacherSubjects_TeacherSubjectId",
                schema: "course",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "TeacherSubjectId",
                schema: "course",
                table: "Courses",
                newName: "SubjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Courses_TeacherSubjectId",
                schema: "course",
                table: "Courses",
                newName: "IX_Courses_SubjectId");

            migrationBuilder.AddColumn<int>(
                name: "CurriculumId",
                schema: "course",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DomainId",
                schema: "course",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GradeId",
                schema: "course",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LevelId",
                schema: "course",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CurriculumId",
                schema: "course",
                table: "Courses",
                column: "CurriculumId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DomainId",
                schema: "course",
                table: "Courses",
                column: "DomainId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_GradeId",
                schema: "course",
                table: "Courses",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_LevelId",
                schema: "course",
                table: "Courses",
                column: "LevelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Curriculums_CurriculumId",
                schema: "course",
                table: "Courses",
                column: "CurriculumId",
                principalSchema: "education",
                principalTable: "Curriculums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_EducationDomains_DomainId",
                schema: "course",
                table: "Courses",
                column: "DomainId",
                principalSchema: "education",
                principalTable: "EducationDomains",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_EducationLevels_LevelId",
                schema: "course",
                table: "Courses",
                column: "LevelId",
                principalSchema: "education",
                principalTable: "EducationLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Grades_GradeId",
                schema: "course",
                table: "Courses",
                column: "GradeId",
                principalSchema: "education",
                principalTable: "Grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Subjects_SubjectId",
                schema: "course",
                table: "Courses",
                column: "SubjectId",
                principalSchema: "education",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
