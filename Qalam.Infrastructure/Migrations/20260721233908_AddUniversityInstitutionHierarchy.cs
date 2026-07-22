using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniversityInstitutionHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AcademicTerms_CurriculumId_NameEn",
                schema: "education",
                table: "AcademicTerms");

            migrationBuilder.AddColumn<int>(
                name: "AcademicProgramId",
                schema: "education",
                table: "Subjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UniversityId",
                schema: "education",
                table: "Subjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcademicProgramId",
                schema: "student",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CollegeId",
                schema: "student",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                schema: "student",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UniversityId",
                schema: "student",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FlagEmoji",
                schema: "common",
                table: "Nationalities",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcademicTermOptional",
                schema: "teaching",
                table: "EducationRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasAcademicProgram",
                schema: "teaching",
                table: "EducationRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasCollege",
                schema: "teaching",
                table: "EducationRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasDepartment",
                schema: "teaching",
                table: "EducationRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasUniversity",
                schema: "teaching",
                table: "EducationRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AcademicProgramId",
                schema: "education",
                table: "EducationLevels",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CurriculumId",
                schema: "education",
                table: "AcademicTerms",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "AcademicProgramId",
                schema: "education",
                table: "AcademicTerms",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Universities",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Universities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Colleges",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniversityId = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colleges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Colleges_Universities_UniversityId",
                        column: x => x.UniversityId,
                        principalSchema: "education",
                        principalTable: "Universities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollegeId = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Colleges_CollegeId",
                        column: x => x.CollegeId,
                        principalSchema: "education",
                        principalTable: "Colleges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicPrograms",
                schema: "education",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DegreeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicPrograms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicPrograms_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "education",
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_AcademicProgramId",
                schema: "education",
                table: "Subjects",
                column: "AcademicProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_UniversityId",
                schema: "education",
                table: "Subjects",
                column: "UniversityId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_AcademicProgramId",
                schema: "student",
                table: "Students",
                column: "AcademicProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_CollegeId",
                schema: "student",
                table: "Students",
                column: "CollegeId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_DepartmentId",
                schema: "student",
                table: "Students",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_UniversityId_CollegeId_DepartmentId_AcademicProgramId",
                schema: "student",
                table: "Students",
                columns: new[] { "UniversityId", "CollegeId", "DepartmentId", "AcademicProgramId" });

            migrationBuilder.CreateIndex(
                name: "IX_EducationLevels_AcademicProgramId",
                schema: "education",
                table: "EducationLevels",
                column: "AcademicProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationLevels_AcademicProgramId_NameEn",
                schema: "education",
                table: "EducationLevels",
                columns: new[] { "AcademicProgramId", "NameEn" },
                unique: true,
                filter: "[AcademicProgramId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicTerms_AcademicProgramId",
                schema: "education",
                table: "AcademicTerms",
                column: "AcademicProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicTerms_AcademicProgramId_NameEn",
                schema: "education",
                table: "AcademicTerms",
                columns: new[] { "AcademicProgramId", "NameEn" },
                unique: true,
                filter: "[AcademicProgramId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicTerms_CurriculumId_NameEn",
                schema: "education",
                table: "AcademicTerms",
                columns: new[] { "CurriculumId", "NameEn" },
                unique: true,
                filter: "[CurriculumId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPrograms_DepartmentId",
                schema: "education",
                table: "AcademicPrograms",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPrograms_DepartmentId_Code",
                schema: "education",
                table: "AcademicPrograms",
                columns: new[] { "DepartmentId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPrograms_IsActive",
                schema: "education",
                table: "AcademicPrograms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Colleges_IsActive",
                schema: "education",
                table: "Colleges",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Colleges_UniversityId",
                schema: "education",
                table: "Colleges",
                column: "UniversityId");

            migrationBuilder.CreateIndex(
                name: "IX_Colleges_UniversityId_Code",
                schema: "education",
                table: "Colleges",
                columns: new[] { "UniversityId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CollegeId",
                schema: "education",
                table: "Departments",
                column: "CollegeId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CollegeId_Code",
                schema: "education",
                table: "Departments",
                columns: new[] { "CollegeId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_IsActive",
                schema: "education",
                table: "Departments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Universities_Code",
                schema: "education",
                table: "Universities",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Universities_IsActive",
                schema: "education",
                table: "Universities",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Universities_NameEn",
                schema: "education",
                table: "Universities",
                column: "NameEn");

            migrationBuilder.AddForeignKey(
                name: "FK_AcademicTerms_AcademicPrograms_AcademicProgramId",
                schema: "education",
                table: "AcademicTerms",
                column: "AcademicProgramId",
                principalSchema: "education",
                principalTable: "AcademicPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EducationLevels_AcademicPrograms_AcademicProgramId",
                schema: "education",
                table: "EducationLevels",
                column: "AcademicProgramId",
                principalSchema: "education",
                principalTable: "AcademicPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_AcademicPrograms_AcademicProgramId",
                schema: "student",
                table: "Students",
                column: "AcademicProgramId",
                principalSchema: "education",
                principalTable: "AcademicPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Colleges_CollegeId",
                schema: "student",
                table: "Students",
                column: "CollegeId",
                principalSchema: "education",
                principalTable: "Colleges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Departments_DepartmentId",
                schema: "student",
                table: "Students",
                column: "DepartmentId",
                principalSchema: "education",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Universities_UniversityId",
                schema: "student",
                table: "Students",
                column: "UniversityId",
                principalSchema: "education",
                principalTable: "Universities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_AcademicPrograms_AcademicProgramId",
                schema: "education",
                table: "Subjects",
                column: "AcademicProgramId",
                principalSchema: "education",
                principalTable: "AcademicPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Universities_UniversityId",
                schema: "education",
                table: "Subjects",
                column: "UniversityId",
                principalSchema: "education",
                principalTable: "Universities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcademicTerms_AcademicPrograms_AcademicProgramId",
                schema: "education",
                table: "AcademicTerms");

            migrationBuilder.DropForeignKey(
                name: "FK_EducationLevels_AcademicPrograms_AcademicProgramId",
                schema: "education",
                table: "EducationLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_AcademicPrograms_AcademicProgramId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Colleges_CollegeId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Departments_DepartmentId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Universities_UniversityId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_AcademicPrograms_AcademicProgramId",
                schema: "education",
                table: "Subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Universities_UniversityId",
                schema: "education",
                table: "Subjects");

            migrationBuilder.DropTable(
                name: "AcademicPrograms",
                schema: "education");

            migrationBuilder.DropTable(
                name: "Departments",
                schema: "education");

            migrationBuilder.DropTable(
                name: "Colleges",
                schema: "education");

            migrationBuilder.DropTable(
                name: "Universities",
                schema: "education");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_AcademicProgramId",
                schema: "education",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_UniversityId",
                schema: "education",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Students_AcademicProgramId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_CollegeId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_DepartmentId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_UniversityId_CollegeId_DepartmentId_AcademicProgramId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_EducationLevels_AcademicProgramId",
                schema: "education",
                table: "EducationLevels");

            migrationBuilder.DropIndex(
                name: "IX_EducationLevels_AcademicProgramId_NameEn",
                schema: "education",
                table: "EducationLevels");

            migrationBuilder.DropIndex(
                name: "IX_AcademicTerms_AcademicProgramId",
                schema: "education",
                table: "AcademicTerms");

            migrationBuilder.DropIndex(
                name: "IX_AcademicTerms_AcademicProgramId_NameEn",
                schema: "education",
                table: "AcademicTerms");

            migrationBuilder.DropIndex(
                name: "IX_AcademicTerms_CurriculumId_NameEn",
                schema: "education",
                table: "AcademicTerms");

            migrationBuilder.DropColumn(
                name: "AcademicProgramId",
                schema: "education",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "UniversityId",
                schema: "education",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "AcademicProgramId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "CollegeId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "UniversityId",
                schema: "student",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "AcademicTermOptional",
                schema: "teaching",
                table: "EducationRules");

            migrationBuilder.DropColumn(
                name: "HasAcademicProgram",
                schema: "teaching",
                table: "EducationRules");

            migrationBuilder.DropColumn(
                name: "HasCollege",
                schema: "teaching",
                table: "EducationRules");

            migrationBuilder.DropColumn(
                name: "HasDepartment",
                schema: "teaching",
                table: "EducationRules");

            migrationBuilder.DropColumn(
                name: "HasUniversity",
                schema: "teaching",
                table: "EducationRules");

            migrationBuilder.DropColumn(
                name: "AcademicProgramId",
                schema: "education",
                table: "EducationLevels");

            migrationBuilder.DropColumn(
                name: "AcademicProgramId",
                schema: "education",
                table: "AcademicTerms");

            migrationBuilder.AlterColumn<string>(
                name: "FlagEmoji",
                schema: "common",
                table: "Nationalities",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CurriculumId",
                schema: "education",
                table: "AcademicTerms",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicTerms_CurriculumId_NameEn",
                schema: "education",
                table: "AcademicTerms",
                columns: new[] { "CurriculumId", "NameEn" },
                unique: true);
        }
    }
}
