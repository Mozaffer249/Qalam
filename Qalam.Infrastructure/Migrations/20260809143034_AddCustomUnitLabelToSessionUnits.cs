using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Qalam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomUnitLabelToSessionUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomUnitLabel",
                schema: "sr",
                table: "SessionRequestSessionUnits",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomUnitLabel",
                schema: "course",
                table: "CourseSessionUnits",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomUnitLabel",
                schema: "sr",
                table: "SessionRequestSessionUnits");

            migrationBuilder.DropColumn(
                name: "CustomUnitLabel",
                schema: "course",
                table: "CourseSessionUnits");
        }
    }
}
