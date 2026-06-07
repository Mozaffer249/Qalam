using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FilterTeacherRegistrationSubmissionUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeacherRegistrationSubmissions_TeacherId_RequirementId",
                schema: "teacher",
                table: "TeacherRegistrationSubmissions");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherRegistrationSubmissions_TeacherId_RequirementId",
                schema: "teacher",
                table: "TeacherRegistrationSubmissions",
                columns: new[] { "TeacherId", "RequirementId" },
                unique: true,
                filter: "[TeacherDocumentId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeacherRegistrationSubmissions_TeacherId_RequirementId",
                schema: "teacher",
                table: "TeacherRegistrationSubmissions");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherRegistrationSubmissions_TeacherId_RequirementId",
                schema: "teacher",
                table: "TeacherRegistrationSubmissions",
                columns: new[] { "TeacherId", "RequirementId" },
                unique: true);
        }
    }
}
