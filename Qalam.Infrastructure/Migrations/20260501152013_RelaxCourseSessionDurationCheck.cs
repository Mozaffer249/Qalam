using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RelaxCourseSessionDurationCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Course_SessionDuration",
                schema: "course",
                table: "Courses");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Course_SessionDuration",
                schema: "course",
                table: "Courses",
                sql: "(([IsFlexible] = 1) AND ([SessionDurationMinutes] IS NULL)) OR (([IsFlexible] = 0) AND (([SessionDurationMinutes] IS NULL) OR ([SessionDurationMinutes] > 0)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Course_SessionDuration",
                schema: "course",
                table: "Courses");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Course_SessionDuration",
                schema: "course",
                table: "Courses",
                sql: "(([IsFlexible] = 0) AND ([SessionDurationMinutes] IS NOT NULL) AND ([SessionDurationMinutes] > 0)) OR (([IsFlexible] = 1) AND ([SessionDurationMinutes] IS NULL))");
        }
    }
}
