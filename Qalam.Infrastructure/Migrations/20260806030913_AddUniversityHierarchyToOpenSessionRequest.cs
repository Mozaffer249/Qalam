using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniversityHierarchyToOpenSessionRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademicProgramId",
                schema: "sr",
                table: "SessionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CollegeId",
                schema: "sr",
                table: "SessionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                schema: "sr",
                table: "SessionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UniversityId",
                schema: "sr",
                table: "SessionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_AcademicProgramId",
                schema: "sr",
                table: "SessionRequests",
                column: "AcademicProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_CollegeId",
                schema: "sr",
                table: "SessionRequests",
                column: "CollegeId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_DepartmentId",
                schema: "sr",
                table: "SessionRequests",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRequests_UniversityId",
                schema: "sr",
                table: "SessionRequests",
                column: "UniversityId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionRequests_AcademicPrograms_AcademicProgramId",
                schema: "sr",
                table: "SessionRequests",
                column: "AcademicProgramId",
                principalSchema: "education",
                principalTable: "AcademicPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionRequests_Colleges_CollegeId",
                schema: "sr",
                table: "SessionRequests",
                column: "CollegeId",
                principalSchema: "education",
                principalTable: "Colleges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionRequests_Departments_DepartmentId",
                schema: "sr",
                table: "SessionRequests",
                column: "DepartmentId",
                principalSchema: "education",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionRequests_Universities_UniversityId",
                schema: "sr",
                table: "SessionRequests",
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
                name: "FK_SessionRequests_AcademicPrograms_AcademicProgramId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionRequests_Colleges_CollegeId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionRequests_Departments_DepartmentId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionRequests_Universities_UniversityId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropIndex(
                name: "IX_SessionRequests_AcademicProgramId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropIndex(
                name: "IX_SessionRequests_CollegeId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropIndex(
                name: "IX_SessionRequests_DepartmentId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropIndex(
                name: "IX_SessionRequests_UniversityId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropColumn(
                name: "AcademicProgramId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropColumn(
                name: "CollegeId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "sr",
                table: "SessionRequests");

            migrationBuilder.DropColumn(
                name: "UniversityId",
                schema: "sr",
                table: "SessionRequests");
        }
    }
}
