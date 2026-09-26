using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherSampleLessonMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "SampleLessonMediaKind",
                table: "Teachers",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SampleLessonMediaPath",
                table: "Teachers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SampleLessonMediaKind",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "SampleLessonMediaPath",
                table: "Teachers");
        }
    }
}
