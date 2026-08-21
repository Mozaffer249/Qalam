using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFreeSessionPolicyFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasCompletedInterviewSession",
                table: "Teachers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasUsedFreeTrialSession",
                schema: "student",
                table: "Students",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Existing teachers with a level already passed interview.
            migrationBuilder.Sql("""
                UPDATE Teachers SET HasCompletedInterviewSession = 1 WHERE TeacherLevelId IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasCompletedInterviewSession",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "HasUsedFreeTrialSession",
                schema: "student",
                table: "Students");
        }
    }
}
