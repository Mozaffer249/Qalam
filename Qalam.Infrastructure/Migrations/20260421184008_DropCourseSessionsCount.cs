using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropCourseSessionsCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Course_SessionsCount",
                schema: "course",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SessionsCount",
                schema: "course",
                table: "Courses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionsCount",
                schema: "course",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Course_SessionsCount",
                schema: "course",
                table: "Courses",
                sql: "(([IsFlexible] = 0) AND ([SessionsCount] IS NOT NULL) AND ([SessionsCount] > 0)) OR (([IsFlexible] = 1) AND ([SessionsCount] IS NULL))");
        }
    }
}
